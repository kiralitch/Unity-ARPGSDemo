using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_HitReactionState : Player_CommonReactState
{
    public Player_HitReactionState(Player_MovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
    {
        
    }

    public override void Enter()
    {
        base.Enter();

        SetCommonReactionWeight(0);
    }
}
