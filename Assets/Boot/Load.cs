using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HybridCLR;
using UnityEngine;
using YooAsset;

/*
 * 热更新代码 
 * 首先加载下载资源文件，接着初始化补充元数据dll，完成后就开始游戏了
 */

public class Load : MonoBehaviour
{
    //UI显示
    [SerializeField, Header("热更新UI视图")]private HotUpdateVIew hotUpdateVIewUI;
    
    //地址
    [SerializeField, Header("资源系统位置")]private string DefaultHostServer = "http://127.0.0.1/CDN/Android/v1.0";
    [SerializeField, Header("备用位置")]private string FallbackHostServer = "http://127.0.0.1/CDN/Android/v1.0";
    
    private ResourcePackage Package;
    public PlayModeType playmode;
    
    //获取资源二进制
    private static Dictionary<string, byte[]> AssetDatas = new Dictionary<string, byte[]>();
    
    //补充元数据
    //字符串数组，包含了AOT 补充元数据程序集的名字
    public static List<string> AOTMetaAssemblyNames { get; private set; } = new List<string>()
    {
        "mscorlib.dll",
        "System.dll",
        "System.Core.dll", // 如果使用了Linq，需要这个
        //"Newtonsoft.Json.dll",
        //"protobuf-net.dll",
    };

    private void Awake()
    {
        System.Net.WebRequest.DefaultWebProxy = null;
        Environment.SetEnvironmentVariable("HTTP_PROXY", null);
        Environment.SetEnvironmentVariable("HTTPS_PROXY", null);
        
        //第一步：
        //初始化YooAsset
        InitYooAsset();

    }

    private void InitYooAsset()
    {
        YooAssets.Initialize(); //初始化

        Package = YooAssets.CreatePackage("DefaultPackage"); //创建默认资源包
        
        YooAssets.SetDefaultPackage(Package); //设置该资源包为默认资源包 
        StartCoroutine(InitPackage()); //开始协程
    }

    //协程 (Coroutine)  必要
    private IEnumerator InitPackage()
    {
        InitializationOperation operation = null;
        
        switch (playmode)
        {
            case PlayModeType.EditorMode:
                var EditorParameters = new EditorSimulateModeParameters();
                EditorParameters.SimulateManifestFilePath  = EditorSimulateModeHelper.SimulateBuild("DefaultPackage");
                operation = Package.InitializeAsync(EditorParameters);
                break;
            case PlayModeType.SinglePlayMode:
                var SinglePlayParameters = new OfflinePlayModeParameters();
                operation = Package.InitializeAsync(SinglePlayParameters);
                break;
            case PlayModeType.HostPlayMode:
                var HostPlayModeParameters = new HostPlayModeParameters();
                HostPlayModeParameters.BuildinQueryServices = new GameQueryServices();
                HostPlayModeParameters.DeliveryQueryServices = new GameDeliveryQueryServices();
                HostPlayModeParameters.DecryptionServices = new GameDecryptionServices();
                HostPlayModeParameters.RemoteServices = new RemoteServices(DefaultHostServer, FallbackHostServer);
                operation = Package.InitializeAsync(HostPlayModeParameters);
                break;
        }
        
        Debug.Log("开始请求版本文件...");
        
        //等待初始化
        yield return operation;

        //如果是多人游戏时
        //if (playmode == PlayModeType.HostPlayMode)
        //{
            Debug.Log("Operation::" +  operation.Status);
            if (operation.Status != EOperationStatus.Succeed) //初始化结果
            {
                Debug.Log($"{operation.Error}");
                yield break;
            }

            //第二步：
            //获取资源包
            //调用UpdatePackageVersionAsync()获取资源包版本
            var verssionOperation = Package.UpdatePackageVersionAsync(); 
            yield return verssionOperation; //等待完成
            if (verssionOperation.Status != EOperationStatus.Succeed)
            {
                Debug.LogError("version Error:" + verssionOperation.Error);
                yield break;
            }

            //第三步：
            //更新资源清单
            var ManifestOperation = Package.UpdatePackageManifestAsync(verssionOperation.PackageVersion);
            yield return ManifestOperation;
            
            //检测下载结果
            if (ManifestOperation.Status != EOperationStatus.Succeed)
            {
                Debug.LogError("UpdateManifest Error:" + ManifestOperation.Error);
                yield break;
            }

            //第四步：开始下载
            yield return Download();
       // }
    }

    //检测到版本清单后再开始下载资源包
    private IEnumerator Download()
    {
        int downloadingMaxNum = 10; //资源最大数量
        int failedTryAgain = 3; //失败重试的数量
        var package = YooAssets.GetPackage("DefaultPackage");
        var downloader = package.CreateResourceDownloader(downloadingMaxNum, failedTryAgain); //获取资源下载器
        
        //检查是否需要下载资源
        if (downloader.TotalDownloadCount == 0)
        {        
            hotUpdateVIewUI.ReflashUI(1,"目前没有更新!!");
            Debug.Log("无更新，直接进入游戏");
            yield return InitCode();
        }
        
        //需要下载的文件总数和总大小
        int totalDownloadCount = downloader.TotalDownloadCount;
        long totalDownloadBytes = downloader.TotalDownloadBytes;   
        
        //注册回调方法
        downloader.OnDownloadErrorCallback = OnDownloadErrorFunction; //更新错误的回调函数
        downloader.OnDownloadProgressCallback = OnDownloadProgressUpdateFunction; //更新时的回调函数
        downloader.OnDownloadOverCallback = OnDownloadOverFunction; //更新结束的回调函数
        downloader.OnStartDownloadFileCallback = OnStartDownloadFileFunction; //更新开始的回调函数
        
        //开启下载
        downloader.BeginDownload();
        yield return downloader;

        //检测下载结果
        if (downloader.Status == EOperationStatus.Succeed)
        {
            //下载成功
            //第五步：初始化元数据（整合代码）
            Debug.Log("下载成功，准备开始整合代码");
            yield return InitCode();

        }
        else
        {
            //下载失败
            Debug.Log("下载失败");
            yield break;
        }
    }

    //补充元数据
    //这段代码是一个HybridCLR热更新框架中典型的补充元数据并加载热更程序集 的协程方法
    //在游戏启动时，加载热更新所需的补充元数据 DLL，并将主热更 DLL 加载到内存，最后进入游戏
    private IEnumerator InitCode()
    {
        //HybridCLR 要求：
        //使用 LoadMetadataForAOTAssemblies() 之前，必须提前加载这些“裁减后 AOT 程序集的补充元数据”资源，才能正常运行热更代码
        var assets = new List<string>
        {
            "GM_Game.dll" //热更主程序集
        }.Concat(AOTMetaAssemblyNames); //用 Concat 把两部分合并，组成一个完整的加载清单

        foreach (var asset in assets)
        {
            //异步加载 每个 DLL 特别需要注意路径
            var dllHandle = Package.LoadAssetAsync<TextAsset>("Assets/GM_Game/Dlls/" + asset);
            yield return dllHandle; //等待异步加载完成
            TextAsset textAsset = dllHandle.AssetObject as TextAsset; 
            AssetDatas[asset] = textAsset.bytes; //加载完成后，取出字节数组，存入 AssetDatas 字典
            Debug.Log($"dll:{asset} size:{textAsset.bytes.Length}");
        }
        
        LoadMetadataForAOTAssemblies();
#if !UNITY_EDITOR
//编辑器环境下GM_Game.dll.bytes已经被加载，不需要再加载
    Assembly.Load(AssetDatas["GM_Game.dll"]);
#endif
        //第六步：进入游戏
        yield return EnterGame();
    }

    //进入游戏
    private IEnumerator EnterGame()
    {
        Debug.Log(" 加载完成，EnterGame");
        //LoadSceneAsync 余异步加载场景
        SceneOperationHandle SceneHandle = Package.LoadSceneAsync("Assets/GM_Game/Scenes/Scene_Login"); //输入资产文件路径
        yield return SceneHandle;

    }

    //AOT 程序集补充元数据
    private static void LoadMetadataForAOTAssemblies()
    {
        //补充元数据是给AOT dll补充元数据，不是给热更新dll补充元数据
        //热更新dll不缺元数据，不需要额外补充，如果要调用LoadMetadataForAOTAssembly会返回错误
        HomologousImageMode mode = HomologousImageMode.SuperSet; //HybridCLR 定义的枚举，用于指定元数据匹配模式 SuperSet：超集
        foreach (var aotDllName in AOTMetaAssemblyNames)
        {
            //从 AssetDatas 字典中取出对应名称的 DLL 字节数组 
            byte[] dllBytes = AssetDatas[aotDllName];
            // 执行补充元数据时内部会自动将dllBytes复制一份，调用完成后请不要将dllBytes保存，造成无谓的内存浪费
            LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, mode);
            Debug.Log($"LoadMetadataForAOTAssembly:{aotDllName}. mode:{mode}. ret:{err}");
        }
    }

    /*
     * 回调函数区域
     */
    
    //下载进度
    private void OnStartDownloadFileFunction(string fileName, long sizeBytes)
    {
        Debug.Log($"开始下载：{fileName} 大小：{sizeBytes / 1024f}KB");
    }

    //下载结果
    private void OnDownloadOverFunction(bool isSucceed)
    {
        Debug.Log($"下载" + (isSucceed ? "成功" : "失败"));
    }

    //下载更新
    private void OnDownloadProgressUpdateFunction(int totalDownloadCount, int currentDownloadCount, long totalDownloadBytes, long currentDownloadBytes)
    {
        Debug.Log($"文件总数:: {totalDownloadBytes} 当前文件数:: {currentDownloadBytes} 总大小：{totalDownloadBytes} 当前下载的大小：{currentDownloadCount}");

        float Progres = currentDownloadBytes * 1.0f / totalDownloadBytes; //总进度条（0-1）
        
        //下载更新进度条 
        //总进度除以1024是kb，除以2048是Mb    
        hotUpdateVIewUI.ReflashUI(
            Progres,
            $"下载进度::{currentDownloadBytes / 1024 / 1024}M /" +
            $" {totalDownloadBytes / 1024 / 1024} {Progres * 100} % "
            );
    }

    //下载失败
    private void OnDownloadErrorFunction(string fileName, string error)
    {
        Debug.Log($"下载{fileName}失败， Error:{error}");
    }
}

/*
 * 下面是要用到的接口实现
 *
 */

internal class GameDeliveryQueryServices : IDeliveryQueryServices
{
    private string GetPackageRoot(string packageName)
    {
        // 这个路径必须和 YooAsset 实际缓存的目录一致
        return Path.Combine(Application.persistentDataPath, "YooAsset", packageName);
    }
    
    public bool QueryDeliveryFiles(string packageName, string fileName)
    {
        return false;
    }

    public DeliveryFileInfo GetDeliveryFileInfo(string packageName, string fileName)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "yoo", packageName ,fileName);
        bool exists = File.Exists(filePath);
        return new DeliveryFileInfo
        {
            DeliveryFilePath = exists ? filePath : null,
            DeliveryFileOffset = 0,
        };
    }
}

internal class GameQueryServices : IBuildinQueryServices
{
    public bool QueryStreamingAssets(string packageName, string fileName)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "yoo", packageName ,fileName);
        return File.Exists(filePath);
    }
}

internal class RemoteServices : IRemoteServices
{
    private readonly string _defaultHostServer;
    private readonly string _fallbackHostServer;
    
    public RemoteServices(string defaultHostServer, string fallbackHostServer)
    {
        _defaultHostServer = defaultHostServer;
        _fallbackHostServer = fallbackHostServer;
    }

    public string GetRemoteMainURL(string fileName)
    {
        return $"{_defaultHostServer}/{fileName}";
    }

    public string GetRemoteFallbackURL(string fileName)
    {
        return $"{_fallbackHostServer}/{fileName}";
    }
}

internal class GameDecryptionServices : IDecryptionServices
{
    public ulong LoadFromFileOffset(DecryptFileInfo fileInfo)
    {
        return 32;
    }

    public byte[] LoadFromMemory(DecryptFileInfo fileInfo)
    {
        throw new NotImplementedException();
    }

    public Stream LoadFromStream(DecryptFileInfo fileInfo)
    {
        return new FileStream(fileInfo.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public uint GetManagedReadBufferSize()
    {
        return 1024;
    }
}


