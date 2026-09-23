using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/*
 * 登录视图
 * 登录模块所有管理视图
 */

public class LoginView : UIBase
{
    [SerializeField, Header("登录窗口")] private LoginWindow LoginWindow;
    [SerializeField, Header("注册窗口")] private RegisterWindow RegisterWindow;

    private Dictionary<WindowType, UIBase> windows;
    
    public override void InitView()
    {
        windows = new Dictionary<WindowType, UIBase>
        {
            { WindowType.LoginWindow, LoginWindow },
            { WindowType.RegisterWindow, RegisterWindow }
        };
    }
    
    //根据WindowType来获取对应的Window窗口
    public UIBase GetWindow(WindowType type)
    {
        windows.TryGetValue(type, out var window);
        return window;
        
    }

    public void ShowWindows(WindowType type)
    {
        //激活该window需要先隐藏其他window
        foreach (var window in windows) //c#遍历器
        {
            window.Value.Show(window.Key == type);
        }
    }
}
