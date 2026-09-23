using System.Collections;
using System.Collections.Generic;
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
}
