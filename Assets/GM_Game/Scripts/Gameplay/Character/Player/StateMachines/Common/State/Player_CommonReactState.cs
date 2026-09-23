using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * 通用 受击和死亡
 * 
 */

public class Player_CommonReactState : Player_GroundedState
{
    private CommonActor ExecutorRef;
    
    public Player_CommonReactState(Player_MovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
    {
        
    }

    public override void Enter()
    {
        base.Enter();
        
        Debug.Log("玩家进入受击动画");
    }

    protected void SetCommonReactionWeight(int index)
    {
        stateMachine.playerRef.RigidBody.velocity = Vector3.zero;
        
        stateMachine.playerRef.AnimationClipData.ResetCommonReactionAnimationTime(0);
        stateMachine.playerRef.AnimationClipData.SetCommonReacitonTarget(0);
        stateMachine.playerRef.AnimationClipData.SetRootTarget(0, 0, 1);
    }
    
    public void SetExecutorRef(CommonActor Executor)
    {
        if (Executor == null) return;
        ExecutorRef = Executor;
    }

    public override void OnAnimationTransitionEvent()
    {
        if(stateMachine.ReusableData.MovementInput != Vector2.zero)
            stateMachine.ChangeState(stateMachine.RunningState);
        
        stateMachine.ChangeState(stateMachine.IdleState);
    }
}
