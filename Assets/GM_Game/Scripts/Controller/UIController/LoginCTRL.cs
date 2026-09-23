using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;
using TMPro;
using UnityEngine;

/*
 * 登录控制器 相当于中介，管理着UIRoot和各个Window之间的关联
 * 对视图显示和隐藏
 * TODO数据操作
 */

public class LoginCTRL : CTRL_Base
{
    private LoginView  loginView;
    
    public LoginCTRL(UIBase view) : base(view)
    {
        loginView = view as LoginView;
        loginView.InitView();

        RegisterCommand();
    }

    public override void ShowView()
    {
        loginView.Show(true);
    }

    public override void HideView()
    {
        loginView.Show(false);
    }
    
    public void ShowWindow(WindowType type)
    {
        loginView.ShowWindows(type);
    }

    /* 注册指令集处理接收区域 */
    private void RegisterCommand()
    {
        SocketDispatcher.GetInstance.AddEventHandler(NetDefine.CMD_RegistCode, OnRegisterHandle);
        SocketDispatcher.GetInstance.AddEventHandler(NetDefine.CMD_LoginCode, OnLoginHandle);
    }

    /* 登录请求结果返回数据 */
    private void OnLoginHandle(ByteString data)
    {
        LoginRet Ret = LoginRet.Parser.ParseFrom(data);

        if (Ret != null && Ret.CmdCode == CmdCode.Succeed)
        {
            Debug.Log("登录成功");
            TipsManger.GetInstance.ShowSystemTips("登录成功!!");
            //TODO 进入游戏 
            
        }
        else
        {
            Debug.Log("登录失败" + Ret.ToString());
            TipsManger.GetInstance.ShowSystemTips("登录失败!!");
        }
    }

    //回调函数 处理服务端返回的数据结果
    //首先要获取服务器发来的结果（这个结果是中心服务器发送到登录服务器再发送到unity）
    //判断是否成功
    private void OnRegisterHandle(ByteString data)
    {
        RegisterRet Ret = RegisterRet.Parser.ParseFrom(data);

        if (Ret != null && Ret.CmdCode == CmdCode.Succeed)
        {
            Debug.Log("注册成功");
            TipsManger.GetInstance.ShowSystemTips("注册成功!!");
            ShowWindow(WindowType.LoginWindow);
        }
        else
        {
            Debug.Log("注册失败" + Ret.ToString());
            TipsManger.GetInstance.ShowSystemTips("注册失败!!");
        }
    }
}
