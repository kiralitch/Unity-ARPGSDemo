using UnityEngine;

/*
 * GameObject扩展类
 * 
 */

public static class GameUtils
{
    //设置全局对象激活
    public static void Show(this GameObject gameobject, bool isActive = true)
    {
        if (gameobject == null) return;
        
        gameobject.SetActive(isActive);
    }
}
