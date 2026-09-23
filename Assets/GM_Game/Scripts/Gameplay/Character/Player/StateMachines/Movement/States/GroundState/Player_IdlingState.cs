using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_IdlingState : Player_GroundedState
{
    public Player_IdlingState(Player_MovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
    {
        
    }

    public override void Enter()
    {
        base.Enter();

        SetAnimationInputWeight(PlayableType.Movement, 0, 1);
        
        stateMachine.ReusableData.MovementSpeedModifier = 0f; //值为0则无法移动
        ResetVelocity();
    }

    public override void Exit()
    {
        base.Exit();
        
    }

    public override void Update()
    {
        if (stateMachine.ReusableData.MovementInput == Vector2.zero) return; //速度为0时为不更新状态，即Idle状态

        OnMove();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        if (!IsMovingHorizontally()) return;
        
        ResetVelocity();
    }
}
