using System;
using UnityEngine;
using YooAsset;

/*
 * 全局UI异步加载
 * 
 */

public class Global : MonoBehaviour
{
    public static Global Instance;
    private ResourcePackage Package;

    public ResourcePackage YooPackage { get => Package; }

    private void Awake()
    {
        DontDestroyOnLoad(this);
        Instance = this;
        Package = YooAssets.GetPackage("DefaultPackage"); //调用Load.cs里面创建的包，从包里面选择特定的资产进行加载
        
        NetSocketMannager.GetInstance.Init();
    }

    private void Start()
    {
        if (Instance)
        {
            Debug.Log("UI资产异步加载脚本已启动");
        }
    }

    //退出程序后断开服务端
    private void OnApplicationQuit()
    {
        NetSocketMannager.GetInstance.DisconnectServer();
    }
}
