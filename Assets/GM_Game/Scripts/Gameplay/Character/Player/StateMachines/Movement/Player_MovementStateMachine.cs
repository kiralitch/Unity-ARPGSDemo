using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * 玩家移动状态机
 * 
 */

public class Player_MovementStateMachine : StateMachine
{
    public Player playerRef { get; }
    public Player_StateReusableData ReusableData { get; } //不需要设置的值 
    public Player_GroundedState GroundedState { get; }

    /* Movement状态区域 */
    public Player_IdlingState IdleState { get; }
    public Player_WalkingState WalkingState { get; }
    public Player_RunningState RunningState { get; }
    public Player_RollingState RollingState { get; }
    
    /* 急停动画状态 */
    public Player_LightStoppingState LightStoppingState { get; }
    public Player_MeddileStoppingState MeddileStoppingState { get; }
    
    /* 通用受击动画和死亡动画 */
    public Player_HitReactionState HitReactionState { get; }

    public Player_MovementStateMachine(Player player)
    {
        playerRef = player; //将玩家引用传入进玩家状态机
        ReusableData = new Player_StateReusableData();
        
        IdleState = new Player_IdlingState(this);
        WalkingState = new Player_WalkingState(this);
        RunningState = new Player_RunningState(this);
        RollingState = new Player_RollingState(this);

        LightStoppingState = new Player_LightStoppingState(this);
        MeddileStoppingState = new Player_MeddileStoppingState(this);
        GroundedState = new Player_GroundedState(this);

        HitReactionState = new Player_HitReactionState(this);
    }
}
