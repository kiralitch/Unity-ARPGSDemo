

public class Player_MeddileStoppingState : Player_StoppingState
{
    public Player_MeddileStoppingState(Player_MovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
    {
    }
    
    public override void Enter()
    {
        base.Enter();

        SetAnimationInputWeight(PlayableType.Movement, 2, 1);
        
        stateMachine.ReusableData.MovementDecelerattionForce = MovementData.StopData.MeddileDecelerationForce;
    }

    public override void Exit()
    {
        base.Exit();
        
    }
}
