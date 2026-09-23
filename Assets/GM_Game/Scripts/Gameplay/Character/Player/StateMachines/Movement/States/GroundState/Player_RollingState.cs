using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_RollingState : Player_GroundedState
{
    private Player_RollingData rollingData;
    private bool bCanAcceptTransitionEvent;
    
    public Player_RollingState(Player_MovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
    {
        rollingData = MovementData.RollingData;
    }

    public override void Enter()
    {
        base.Enter();

        bCanAcceptTransitionEvent = false;
        
        SetAnimationInputWeight(PlayableType.Movement, 4, 1);
        stateMachine.ReusableData.MovementSpeedModifier = rollingData.SpeedModifier; //直接设置速度修正
        AddForceOnTransitionFromStationaryState();
    }

    public override void Update()
    {
        base.Update();
        
        //动画被重置后至少经过一帧，之后再来的过渡通知才是本段翻滚自己的
        bCanAcceptTransitionEvent = true;
    }

    public override void OnAnimationTransitionEvent()
    {
        //丢弃进入翻滚瞬间的残留通知，修复"要按两次才能翻滚"
        if (!bCanAcceptTransitionEvent) return; 
        
        base.OnAnimationTransitionEvent();

        if (stateMachine.ReusableData.MovementInput == Vector2.zero)
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

    protected override void OnMovementCanceled(InputAction.CallbackContext context)
    {
        
    }

    protected override void OnRollingStarted(InputAction.CallbackContext context)
    {
        
    }
}
