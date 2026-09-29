using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Death : Enemy_GroundedState
{
    private bool bDestroyRequested = false;

    public Enemy_Death(Enemy_MovementStateMachine StateMachineRef) : base(StateMachineRef)
    {
    }

    public override void Enter()
    {
        base.Enter();

        bDestroyRequested = false;

        stateMachine.EnemyRef.CurrentStateMode = EnemyStateMode.Death;
        
        stateMachine.EnemyRef.AnimationClipData.ClearEnemyCombo(stateMachine.EnemyRef.AnimationGraph);

        //stateMachine.EnemyRef.AnimationClipData.ResetCommonReactionAnimationTime(1);
        stateMachine.EnemyRef.AnimationClipData.SetCommonReacitonTarget(1);
        stateMachine.EnemyRef.AnimationClipData.SetRootTarget(0, 0, 1);
    }

    public override void PhysicsUpdate()
    {
        //base.PhysicsUpdate();

        stateMachine.EnemyRef.AnimationClipData.SetRootTarget(0, 0, 1);
    }

    public override void OnAnimatationExitEvent()
    {
        //死亡摧毁：动画播完由关键帧事件回调到这里
        DestroyEnemy();
    }
    
    private void DestroyEnemy()
    {
        if (bDestroyRequested) return;

        Enemy EnemyRef = stateMachine?.EnemyRef;
        if (EnemyRef == null) return;

        bDestroyRequested = true;

        EnemyRef.DestroyOnDeath();
    }
}
