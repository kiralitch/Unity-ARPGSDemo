using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_HitReact : Enemy_GroundedState
{
    private CommonActor ExecutorRef;
    
    public Enemy_HitReact(Enemy_MovementStateMachine StateMachineRef) : base(StateMachineRef)
    {
        
    }

    public override void Enter()
    {
        base.Enter();

        /* 受击期间不再追击/攻击，这里停掉 Agent，否则受击动画会推着敌人继续滑向玩家 */
        StopAgentMovement();

        stateMachine.EnemyRef.AnimationClipData.ResetCommonReactionAnimationTime(0);
        stateMachine.EnemyRef.AnimationClipData.SetCommonReacitonTarget(0);
        stateMachine.EnemyRef.AnimationClipData.SetRootTarget(0, 0, 1);
    }

    public override void Exit()
    {
        base.Exit();

        ResumeAgentMovement();
    }

    /* 受击状态覆盖基类的 PhysicsUpdate：基类会在玩家进入攻击范围时触发攻击，
       受击动画播完立刻接一段攻击，表现上就是「每次受击后都打一下」。
       受击期间完全不参与追击与攻击判定。 */
    public override void PhysicsUpdate()
    {
    }

    public override void OnAnimationTransitionEvent()
    {
        base.OnAnimationTransitionEvent();

        stateMachine.ChangeState(stateMachine.IdleState);
    }

    /* 停住 NavMeshAgent，受击位移完全由动画负责 */
    private void StopAgentMovement()
    {
        var Agent = stateMachine.EnemyRef.AIController?.Agent;
        if (Agent == null || !Agent.enabled) return;

        if (Agent.isOnNavMesh) Agent.ResetPath();

        Agent.updateRotation = false;
        Agent.isStopped = true;
    }

    /* 恢复 Agent 的常规控制 */
    private void ResumeAgentMovement()
    {
        var Agent = stateMachine.EnemyRef.AIController?.Agent;
        if (Agent == null || !Agent.enabled) return;

        Agent.isStopped = false;
        Agent.updateRotation = true;
    }

    public void SetExecutorRef(CommonActor Executor)
    {
        if (Executor == null) return;
        ExecutorRef = Executor;
    }
}
