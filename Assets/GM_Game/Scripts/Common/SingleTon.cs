using UnityEngine;

/*
 * 单例基类
 * 用于各种实例化
 */

public class SingleTon <T> where T : new() //泛型约束，T必须有一个公共的无参数构造函数
{
    private static T Instance;
    private static object InstanceLock = new object(); //线程同步的锁对象
    
    
    public static T GetInstance
    {
        get
        {
            if (Instance == null)
            {
                lock (InstanceLock) //检查后加锁，防止多线程同时访问
                {
                    if (Instance == null) {  Instance = new T(); }
                }
            }
            
            return Instance;
        }
    }
}
