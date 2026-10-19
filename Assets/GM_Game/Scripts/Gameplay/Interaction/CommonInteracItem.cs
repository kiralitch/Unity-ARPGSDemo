using System;
using UnityEngine;

/*
 * 通用可交互物品
 *
 */

public abstract class CommonInteracItem : MonoBehaviour
{
    //检测玩家重合
    protected Collider InteractCollider;

    //初始化碰撞器和 UI
    protected virtual void Init()
    {
        if (InteractCollider != null)
        {
            InteractCollider.isTrigger = true;
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        Debug.Log("已进入交互物品范围内");
        
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        Debug.Log("已离开交互物品范围内");
    }

    public virtual void Interact()
    {
        
    }
}
