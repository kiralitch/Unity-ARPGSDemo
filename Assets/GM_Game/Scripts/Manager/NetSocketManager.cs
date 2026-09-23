using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Google.Protobuf;
using UnityEngine;

/*
 * 网络模块管理类
 * 
 */

public class NetSocketMannager : SingleTon<NetSocketMannager>
{
    //创建客户端类
    private static NetClient client;
    private SynchronizationContext synchronizationContext;
    
    public static NetClient GetClient
    {
        get => client;
    }

    public void Init()
    {
        synchronizationContext = SynchronizationContext.Current;
        
        ConnectServer(NetDefine.IPHost, NetDefine.LoginServerPort);
    }

    public void ConnectServer(string IP, int Port)
    {
        DisconnectServer();
        
        client = new NetClient(IP, Port, ClientType.Unity);
        //每当 NetClient 收到服务器发来的完整消息包，就会触发这个事件，从而调用 OnReceiveMessageHandle
        client.OnReceiveMessage += OnReceiveMessageHandle; //从Net模块里写的委托，用于接受服务端发来的数据
        
        client.StartConnect();
    }

    //将事件派发出去
    private void OnReceiveMessageHandle(int protoCode, ByteString data)
    {
        //将子线程切换为主线程 回调函数里执行的是需要切换主线程的函数
        synchronizationContext.Post(_ =>
        {
            SocketDispatcher.GetInstance.DispatherEvent(protoCode, data);
            
        }, null);
    }

    public void DisconnectServer()
    {
        if (client != null)
        {
            client.bIsNeedReconnect = false;
            client.Disconnect();
            client = null;
        }
        
    }
}
