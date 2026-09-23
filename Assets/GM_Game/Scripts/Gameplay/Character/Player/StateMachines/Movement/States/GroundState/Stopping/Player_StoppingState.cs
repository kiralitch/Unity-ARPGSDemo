using UnityEngine.InputSystem;

public class Player_StoppingState : Player_GroundedState
{
    public Player_StoppingState(Player_MovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
    {
        
    }

    public override void Enter()
    {
        base.Enter();

        //如果已经进入战斗模式，不再设置移动动画
        if (stateMachine.playerRef.CurrentStateMode != PlayStateMode.Movement)
            return; 
        
        SetAnimationInputWeight(PlayableType.Movement, 2, 1);
        
        stateMachine.ReusableData.MovementSpeedModifier = 0f;
    }

    public override void Exit()
    {
        base.Exit();
        
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        RotateTowardTargerRoatation();
        
        if (!IsMovingHorizontally()) return;  //如果正在移动则不执行
        
        DecelerateHorizontally(); //开始减速
    }

    /* 结束后切换为待机状态 */
    public override void OnAnimationTransitionEvent()
    {
        stateMachine.ChangeState(stateMachine.IdleState);
    }

    protected override void AddInputActionsCallback()
    {
        base.AddInputActionsCallback();

        stateMachine.playerRef.playerInput.PlayerActions.Movement.started += OnMovementStarted;
    }

    protected override void RemoveInputActionsCallback()
    {
        base.RemoveInputActionsCallback();
        
        stateMachine.playerRef.playerInput.PlayerActions.Movement.started -= OnMovementStarted;
    }

    protected override void OnMovementCanceled(InputAction.CallbackContext context)
    {
        
        
    }
    
    /* 当进入停止动画时是可以被打断的 */
    private void OnMovementStarted(InputAction.CallbackContext context)
    {
        OnMove();
    }
}
