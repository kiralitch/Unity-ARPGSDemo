using System.Collections.Generic;
using System.ComponentModel;
using Google.Protobuf;
using UnityEngine;

/*
 * 把服务端发到的数据派发到客户端上
 * 这是一个存放指令的类
 */

public delegate void OnActionHandle(ByteString data);

public class SocketDispatcher : SingleTon<SocketDispatcher>
{
    private Dictionary<int, OnActionHandle> ActionDIC = new Dictionary<int, OnActionHandle>();

    /* 注册事件 */
    public void AddEventHandler(int protoCode, OnActionHandle handler)
    {
        if (!ActionDIC.ContainsKey(protoCode) && handler != null)
        {
            ActionDIC.Add(protoCode, handler);
        }
    }

    /* 删除事件 */
    public void RemoveEventHandler(int protoCode)
    {
        if (ActionDIC.ContainsKey(protoCode))
        {
            ActionDIC.Remove(protoCode);
        }
    }

    /* 派发事件 */
    public void DispatherEvent(int protoCode, ByteString data)
    {
        if (ActionDIC.ContainsKey(protoCode))
        {
            ActionDIC[protoCode]?.Invoke(data); //将数据取出委托 根据传入的协议号，找到对应的处理函数并执行，把数据作为参数传进去
        }
    }


}
