using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_RollingState : Player_GroundedState
{
    private Player_RollingData rollingData;
    
    private float TransitionEventIgnoreTime = 0.15f; //进状态后忽略这段时间内的动画通知
    private float TransitionEventTimer;
    
    public Player_RollingState(Player_MovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
    {
        rollingData = MovementData.RollingData;
    }

    public override void Enter()
    {
        base.Enter();

        TransitionEventTimer = TransitionEventIgnoreTime;
        
        SetAnimationInputWeight(PlayableType.Movement, 4, 1);
        stateMachine.ReusableData.MovementSpeedModifier = rollingData.SpeedModifier; //直接设置速度修正
        AddForceOnTransitionFromStationaryState();
    }

    public override void Update()
    {
        base.Update();

        if (TransitionEventTimer > 0f) TransitionEventTimer -= Time.deltaTime;
    }

    public override void PhysicsUpdate()
    {
        FloatCapsule();
        RollingMove();
    }

    public override void OnAnimationTransitionEvent()
    {
        //上段动画的残留通知可能持续半秒，按时间窗口丢弃，不用帧数判断
        if (TransitionEventTimer > 0f) return; 
        
        //base.OnAnimationTransitionEvent();

        //不要读 ReusableData.MovementInput：攻击期间 MovementStateMachine 不再调用
        //HandleInput，这个值会冻结在攻击前的旧值上，导致翻滚结束后的分支判断错误。
        //这里直接读当前输入设备状态
        if (stateMachine.playerRef.playerInput.PlayerActions.Movement.ReadValue<Vector2>() == Vector2.zero)
        {
            stateMachine.ChangeState(stateMachine.LightStoppingState);
            return;
        }
        
        stateMachine.ChangeState(stateMachine.RunningState);
    }

    private void AddForceOnTransitionFromStationaryState()
    {
        if (stateMachine.ReusableData.MovementInput != Vector2.zero) return;

        Vector3 CharacterRotationDirection = stateMachine.playerRef.transform.forward; //获取玩家向前方向
        
        CharacterRotationDirection.y = 0;

        stateMachine.playerRef.RigidBody.velocity = CharacterRotationDirection * GetSpeedMovement();
    }

    /* 翻滚期间自己维持水平速度，不依赖 MovementInput，也不受 IdleState 清速度影响 */
    private void RollingMove()
    {
        Vector3 Direction = stateMachine.playerRef.transform.forward;
        Direction.y = 0f;
        Direction.Normalize();

        Vector3 HorizontalVel = GetPlayerHorizontalVelocity();

        stateMachine.playerRef.RigidBody.AddForce(
            Direction * GetSpeedMovement() - HorizontalVel,
            ForceMode.VelocityChange
            );
    }

    protected override void OnMovementCanceled(InputAction.CallbackContext context)
    {
        
    }

    protected override void OnRollingStarted(InputAction.CallbackContext context)
    {
        
    }
}
