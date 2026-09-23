using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * 状态机共享类
 * 
 */

public abstract class StateMachine
{
    protected IState CurrentState; //目前的状态

    public virtual void ChangeState(IState newState)
    {
        /*if (CurrentState != null)
        {
            CurrentState.Exit();
        }*/

        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState.Enter();
    }

    public IState GetCurrentState()
    {
        return CurrentState;
    }

    public void HandleInput()
    {
        CurrentState?.HandleInput();
    }

    public void Update()
    {
        CurrentState?.Update();
    }

    public void PhysicsUpdate()
    {
        CurrentState?.PhysicsUpdate();
    }

    public void OnAnimationEnterEvent()
    {
        CurrentState?.OnAnimatationEnterEvent();
    }

    public void OnAnimationExitEvent()
    {
        CurrentState?.OnAnimatationExitEvent();
    }

    public void OnAnimationTransitionEvent()
    {
        CurrentState?.OnAnimationTransitionEvent();
    }
}
