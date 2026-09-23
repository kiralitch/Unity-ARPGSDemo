using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_MovementStateMachine : StateMachine
{
    public Enemy EnemyRef { get; }
    
    public Enemy_IdleState IdleState { get; }
    public Enemy_RunningState RunningState { get; }

    //受击与死亡
    public Enemy_HitReact HitReactState { get; }

    public Enemy_MovementStateMachine(Enemy enemy)
    {
        EnemyRef = enemy;

        IdleState = new Enemy_IdleState(this);
        RunningState = new Enemy_RunningState(this);
        
        HitReactState = new Enemy_HitReact(this);
    }
}
