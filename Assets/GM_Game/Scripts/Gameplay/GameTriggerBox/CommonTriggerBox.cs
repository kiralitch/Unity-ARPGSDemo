using UnityEngine;

/*
 * 通用触发方块
 * 
 */

public abstract class CommonTriggerBox : MonoBehaviour
{
    protected virtual void OnTriggerEnter(Collider other)
    {
        Debug.Log("进入触发方块");
    }

    protected virtual void OnTriggerStay(Collider other)
    {
        Debug.Log("正在触发方块");
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        Debug.Log("退出触发方块");
    }
}
