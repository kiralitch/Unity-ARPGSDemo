

public class Player_LightStoppingState : Player_StoppingState
{
    public Player_LightStoppingState(Player_MovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        SetAnimationInputWeight(PlayableType.Movement, 2, 1);
        
        stateMachine.ReusableData.MovementDecelerattionForce = MovementData.StopData.LightDecelerationForce;
    }

    public override void Exit()
    {
        base.Exit();
        
    }
}
