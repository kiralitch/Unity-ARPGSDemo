using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;
using TMPro;
using UnityEngine;

/*
 * 注册窗口逻辑
 * 
 */

public class RegisterWindow : UIBase
{
    [SerializeField, Header("账号")] private TMP_InputField Account;
    [SerializeField, Header("密码")] private TMP_InputField Password;
    [SerializeField, Header("确认密码")] private TMP_InputField AgainPassword;

    public void OnRegisterClick()
    {
        //判断是否为空
        if (string.IsNullOrEmpty(Account.text) || string.IsNullOrEmpty(Password.text) ||
            string.IsNullOrEmpty(AgainPassword.text))
        {
            Debug.Log("账号或密码为空");
            return;
        }
        //验证账号密码
        
        //判读两密码是否一致
        if (!Password.text.Equals(AgainPassword.text))
        {
            Debug.Log("密码不一致");
            return;
        }

        //注册
        //TODO
        //Show(false);
        //首先创建注册请求
        RegistReq RegisterReq = new RegistReq()
        {
            UserName = Account.text,
            Password = Password.text,
        };
        
        //发送数据，变成ByteString发送至服务端 
        NetSocketMannager.GetClient.SendData(NetDefine.CMD_RegistCode, RegisterReq.ToByteString());
        //Debug.Log("注册成功");
    }

    public void OnVerifyCodeBtnClicked()
    {
        Debug.Log("注册成功");
    }

    public void OnBackToLoginBtnClicked()
    {
        UIRoot.instance.loginCtrl.ShowWindow(WindowType.LoginWindow);
    }
}
