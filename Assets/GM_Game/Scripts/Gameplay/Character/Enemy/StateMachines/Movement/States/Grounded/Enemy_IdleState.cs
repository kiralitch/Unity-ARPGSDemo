using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_IdleState : Enemy_GroundedState
{
    public Enemy_IdleState(Enemy_MovementStateMachine StateMachineRef) : base(StateMachineRef)
    {
        
    }

    public override void Enter()
    {
        base.Enter();

        //stateMachine.EnemyRef.AIController.ResetAllBlackboard();
        //stateMachine.EnemyRef.AIController.SetBlackBoardValue("Idle", true);
        SetPlayableMovementInput(0, 1f);
    }

    public override void Update()
    {
        /* 攻击/受击期间黑板里 Idle 与 ChasePlayer 都会被清掉，
           此时若继续按「Idle 不为真就切走」判断，会立刻切到 Running，
           而 Running 又因为 ChasePlayer 为假立刻切回来，两帧一循环造成抽搐。
           战斗接管期间移动状态机完全冻结 */
        if (IsCombatBusy()) return;

        if (stateMachine.EnemyRef.AIController.GetBlackBoardValue("Idle")) return;

        ChangeMoveState();
    }
}
