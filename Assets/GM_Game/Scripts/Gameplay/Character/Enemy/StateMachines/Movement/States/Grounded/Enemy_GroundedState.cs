using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_GroundedState : Enemy_MovementState
{
    public Enemy_GroundedState(Enemy_MovementStateMachine StateMachineRef) : base(StateMachineRef)
    {
        
    }

    protected virtual void ChangeMoveState()
    {
        stateMachine.ChangeState(stateMachine.RunningState);
    }
}
