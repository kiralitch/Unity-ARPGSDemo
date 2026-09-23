using UnityEngine.InputSystem;

public class Player_RunningState : PlayerMovingState
{
    public Player_RunningState(Player_MovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
    {
       
    }
    
    public override void Enter()
    {
        base.Enter();
        
        SetAnimationInputWeight(PlayableType.Movement, 1, 1);
        
        stateMachine.ReusableData.MovementSpeedModifier = MovementData.RunData.SpeedModifier; //走路的速率 
    }

    public override void Exit()
    {
        base.Exit();
        
    }

    protected override void OnMovementCanceled(InputAction.CallbackContext context)
    {
        stateMachine.ChangeState(stateMachine.LightStoppingState);
        
        
    }
    
    /* 跑步的时候按了切换就会变成走路 */
    protected override void OnWalkToggleStarted(InputAction.CallbackContext context)
    {
        base.OnWalkToggleStarted(context);
        
        stateMachine.ChangeState(stateMachine.WalkingState);
    }
    
}
