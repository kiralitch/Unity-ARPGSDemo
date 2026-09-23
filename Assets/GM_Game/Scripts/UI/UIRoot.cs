using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * UI管理控制器的管理类
 * 管理控制器
 */

public class UIRoot : MonoBehaviour
{
    //单例模式（Singleton）的全局访问点 实例
    public static UIRoot instance;
    [SerializeField, Header("登录界面")]private LoginView loginView;
    public LoginCTRL loginCtrl;
    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this); //防止切换场景时被销毁

        InitCtrl();
    }

    //初始化控制器
    private void InitCtrl()
    {
        if (loginView == null) return;
        loginCtrl = new LoginCTRL(loginView);
    }
}
