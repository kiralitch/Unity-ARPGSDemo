using YooAsset;
using UnityEngine;

public class GameLauncher : MonoBehaviour
{
    private async void Start()
    {
        // 初始化 YooAssets
        YooAssets.Initialize();

        // 创建资源包
        var package = YooAssets.CreatePackage("DefaultPackage");

        // 设置为默认包（关键）
        YooAssets.SetDefaultPackage(package);

#if UNITY_EDITOR
        // 编辑器模拟模式
        var initParameters = new EditorSimulateModeParameters();
        initParameters.SimulateManifestFilePath = EditorSimulateModeHelper.SimulateBuild("DefaultPackage");
#else
        // 运行时（单机)
        var initParameters = new OfflinePlayModeParameters();
#endif

        // 初始化资源包
        var initOperation = package.InitializeAsync(initParameters);
        await initOperation.Task;

        if (initOperation.Status == EOperationStatus.Succeed)
        {
            Debug.Log("YooAsset 初始化成功，当前模式：" +
#if UNITY_EDITOR
                      "编辑器模拟模式"
#else
                "运行时模式"
#endif
            );
        }
        else
        {
            Debug.LogError($"YooAsset 初始化失败：{initOperation.Error}");
        }
    }
}