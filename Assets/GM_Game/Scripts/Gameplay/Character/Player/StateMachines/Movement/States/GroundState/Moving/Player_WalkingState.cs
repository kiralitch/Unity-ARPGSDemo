using UnityEngine.InputSystem;

public class Player_WalkingState : PlayerMovingState
{
    public Player_WalkingState(Player_MovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
    {
        
    }

    public override void Enter()
    {
        base.Enter();
        
        SetAnimationInputWeight(PlayableType.Movement, 3, 1);
        
        stateMachine.ReusableData.MovementSpeedModifier = MovementData.WalkData.SpeedModifier; //走路的速率 
    }

    public override void Exit()
    {
        base.Exit();
        
    }

    protected override void OnMovementCanceled(InputAction.CallbackContext context)
    {
        stateMachine.ChangeState(stateMachine.LightStoppingState);
        
        
    }

    /* 走路的时候按了切换就会变成跑步 */
    protected override void OnWalkToggleStarted(InputAction.CallbackContext context)
    {
        base.OnWalkToggleStarted(context);
        
        stateMachine.ChangeState(stateMachine.RunningState);
    }
    
}
