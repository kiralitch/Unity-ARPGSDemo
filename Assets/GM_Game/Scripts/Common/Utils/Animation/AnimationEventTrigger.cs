using System;
using UnityEngine;

/*
 * 玩家公用动画通知触发器
 * 
 */

public enum AnimationEventTag
{
    Player,
    Enemy
}

public class CommonAnimationEventTrigger : MonoBehaviour
{
    public AnimationEventTag CurrentAnimationEvent = AnimationEventTag.Player;
    
    private Player PlayerRef;
    private Enemy EnemyRef;

    private void Awake()
    {
        if (CurrentAnimationEvent == AnimationEventTag.Player)
        {
            PlayerRef = transform.parent.GetComponent<Player>(); //获取父类的组件
        }
        else if (CurrentAnimationEvent == AnimationEventTag.Enemy)
        {
            EnemyRef = transform.parent.GetComponent<Enemy>();
        }
    }

    public void TriggerOnMovementStateAnimationEnterEvent()
    {
        if (CurrentAnimationEvent == AnimationEventTag.Player)
            PlayerRef.OnMovementStateAnimationEnterEvent();
        else if (CurrentAnimationEvent == AnimationEventTag.Enemy)
            EnemyRef.OnMovementStateAnimationEnterEvent();
    }

    public void TriggerOnMovementStateAnimationExitEvent()
    {
        if (CurrentAnimationEvent == AnimationEventTag.Player)
            PlayerRef.OnMovementStateAnimationExitEvent();
        else if (CurrentAnimationEvent == AnimationEventTag.Enemy)
            EnemyRef.OnMovementStateAnimationExitEvent();
    }

    public void TriggerOnMovementStateAnimationTransitionEvent()
    {
        if (CurrentAnimationEvent == AnimationEventTag.Player)
            PlayerRef.OnMovementStateAnimationTransitionEvent();
        else if (CurrentAnimationEvent == AnimationEventTag.Enemy)
            EnemyRef.OnMovementStateAnimationTransitionEvent();
    }
}
