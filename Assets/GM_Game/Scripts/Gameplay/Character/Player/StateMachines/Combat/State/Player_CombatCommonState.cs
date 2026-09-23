using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_CombatCommonState : IState
{
    public AbilitiesData CachedAbilitiesData;
    
    protected Player_MovementStateMachine MovementStateMachine;
    protected Player_CombatStateMachine CombatStateMachine;
    protected AnimaClipsData CachedClipsData;

    protected int CombotIndex = 0;
    protected int EndCombotIndex = 0;
    
    public Player_CombatCommonState(Player_MovementStateMachine playerMovementStateMachine, Player_CombatStateMachine playerCombatStateMachine)
    {
        MovementStateMachine = playerMovementStateMachine;
        CombatStateMachine = playerCombatStateMachine;
        CachedClipsData = CombatStateMachine.playerRef.AnimationClipData;
        
    }

    public virtual void Enter()
    {
        Debug.Log("State" + GetType().Name);
        
    }

    public virtual void Exit()
    {
    }

    public virtual void HandleInput()
    {
        
    }

    public virtual void Update()
    {
        
    }

    public virtual void PhysicsUpdate()
    {
        MovementStateMachine.GroundedState.FloatCapsule();
    }
    
    public virtual void OnAnimatationEnterEvent()
    {
        
    }

    public virtual void OnAnimatationExitEvent()
    {
        
    }

    public virtual void OnAnimationTransitionEvent()
    {
        
    }
}
