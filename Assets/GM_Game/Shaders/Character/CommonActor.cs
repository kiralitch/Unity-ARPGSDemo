using UnityEngine;

/*
 * 游戏角色通用类
 * 
 */

public class CommonActor : MonoBehaviour, IDamageable
{
    public virtual void TakeDamage(float damage, CommonActor Executor)
    {
        
    }

    public virtual void PlayHitReaction(CommonActor Executor)
    {
        
    }

    public virtual void PlayDeath()
    {
        Debug.Log("开始播放死亡动画");
    }
    
    /* 死亡处理区域 */
    /* 死亡动画播完后的销毁入口，由动画退出事件里调用。
   这里负责把销毁前必须做干净的收尾步骤一次性做完： */
    public virtual void DestroyOnDeath()
    {
        if (!gameObject.activeInHierarchy) return;
    }
}
