using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_CombatCommonState : IState
{
    protected Enemy CachedEnemy;
    
    protected Enemy_MovementStateMachine MovementStateMachine;
    protected Enemy_CombatStateMachine CombatStateMachine;
    protected AnimaClipsData CachedClipsData;
    
    public Enemy_CombatCommonState(Enemy_MovementStateMachine movementStateMachine, Enemy_CombatStateMachine combatStateMachine)
    {
        CachedEnemy = combatStateMachine.EnemyRef;
        
        MovementStateMachine = movementStateMachine;
        CombatStateMachine = combatStateMachine;
        CachedClipsData = combatStateMachine.EnemyRef.AnimationClipData;
    }

    #region IState接口

    public virtual void Enter()
    {
        
    }

    public virtual void Exit()
    {
        
    }

    public virtual void HandleInput()
    {
        
    }

    public virtual  void Update()
    {
        
    }

    public virtual void PhysicsUpdate()
    {
        
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

    #endregion
    
   
}
