using System;
using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
 * 登录窗口
 * 
 */

public class LoginWindow : UIBase
{
    //存放InputField组件
    [SerializeField, Header("账号输入框")] private TMP_InputField  iptAccount; //账号
    [SerializeField, Header("密码输入框")] private TMP_InputField  iptPassword; //密码
    
    [SerializeField, Header("记住账号选项")] private Toggle TogRememberAcc; //记住账号
    [SerializeField, Header("同意协议选项")] private Toggle TogAgreement; //同意协议

    private void Awake()
    {
        //每次打开游戏获取你的磁盘初始化
        int agreement = PlayerPrefs.GetInt("Agreement");
        if(agreement == 1) TogAgreement.isOn = true;
        
        string Acc = PlayerPrefs.GetString("Account:");
        if (!string.IsNullOrEmpty(Acc)) iptAccount.text = Acc; 
    }

    //跳转注册界面
    public void OnGotoRegisterBtnClicked()
    {
        //跳转注册界面
        UIRoot.instance.loginCtrl.ShowWindow(WindowType.RegisterWindow);
    }

    public void OnLoginBtnClicked()
    {
        //判断输入框是否为空和协议是否勾选 ，并保存到本地 
        if (string.IsNullOrEmpty(iptAccount.text) ||
            string.IsNullOrEmpty(iptPassword.text) ||
            !TogAgreement.isOn)
        {
            Debug.Log("账号密码为空或未勾选协议！");
            TipsManger.GetInstance.ShowSystemTips("账号密码不能为空或未勾选协议!!"); 
            return;
        }
        
        PlayerPrefs.SetInt("Agreement", TogAgreement.isOn ? 1 : 0);
        
        //判断是否勾选记住账号    
        if (TogRememberAcc.isOn)
        {
            PlayerPrefs.SetString("Account:", iptAccount.text);
        }
        else
        {
            PlayerPrefs.SetString("Account:", "");
        }

        PlayerPrefs.Save();

        //服务器验证通过后方可登录
        //TODO
        LoginReq LoginRequeast = new LoginReq()
        {
            UserName =  iptAccount.text,
            Password =  iptPassword.text,
        };
        
        NetSocketMannager.GetClient.SendData(NetDefine.CMD_LoginCode, LoginRequeast.ToByteString());
        
        //Debug.Log("登录成功");
        
    }
    
}
