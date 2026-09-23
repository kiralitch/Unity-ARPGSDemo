using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_RunningState : Enemy_GroundedState
{
    public Enemy_RunningState(Enemy_MovementStateMachine StateMachineRef) : base(StateMachineRef)
    {
        
    }

    public override void Enter()
    {
        base.Enter();
        
        //stateMachine.EnemyRef.AIController.ResetAllBlackboard();
        //stateMachine.EnemyRef.AIController.SetBlackBoardValue("ChasePlayer", true);
        SetPlayableMovementInput(1, 1f);
    }

    public override void Update()
    {
        /* 与 Idle 对称：攻击/受击期间 ChasePlayer 被清掉，
           这里若继续判断就会和 Idle 来回切换，战斗接管期间必须冻结 */
        if (IsCombatBusy()) return;

        if (stateMachine.EnemyRef.AIController.GetBlackBoardValue("ChasePlayer")) return;

        stateMachine.ChangeState(stateMachine.IdleState);
    }
}
