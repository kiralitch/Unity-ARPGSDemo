

public class PlayerMovingState : Player_GroundedState
{
    public PlayerMovingState(Player_MovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
    {
        
    }

    public override void Enter()
    {
        base.Enter();
        
        //SetAnimationInputWeight(PlayableType.Movement, 0, 0);
    }

    public override void Exit()
    {
        base.Exit();
        

    }
}
