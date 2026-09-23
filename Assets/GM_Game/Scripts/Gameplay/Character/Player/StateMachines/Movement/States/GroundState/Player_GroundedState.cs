using UnityEngine;
using UnityEngine.InputSystem;

public class Player_GroundedState : Player_MovementStates
{
    private SlopeData slopeData;
    
    public Player_GroundedState(Player_MovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
    {
        slopeData = stateMachine.playerRef.ColliderUtility.slopeData;
    }

    public override void Enter()
    {
        base.Enter();
        
        //SetAnimationInputWeight(stateMachine.playerRef.AnimationData.GroundedParameterHash);
    }

    public override void Exit()
    {
        base.Exit();
        
        //StopAnimation(stateMachine.playerRef.AnimationData.GroundedParameterHash);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        FloatCapsule();
    }

    /* 浮动胶囊 */
    public void FloatCapsule()
    {
        Vector3 CapsuleColliderCenterInWorldSpace =
            stateMachine.playerRef.ColliderUtility.capsuleColliderData.Collider.bounds.center;
        
        Ray DownforwardsRayFromCapsuleCenter = new Ray(
            CapsuleColliderCenterInWorldSpace,
            Vector3.down
            );

        //射线检测
        bool RayResult = Physics.Raycast(
            DownforwardsRayFromCapsuleCenter,
            out RaycastHit hit,
            slopeData.FloatRayDistance,
            stateMachine.playerRef.LayerData.GroundLayer,  //只检测指定层级
            QueryTriggerInteraction.Ignore  //忽略触发交互的对象
        );

        if (RayResult)
        {
            //通过检测射线向下的法线和玩家向下的法线求出地面的坡度
            float GroundAngle = Vector3.Angle(hit.normal, -DownforwardsRayFromCapsuleCenter.direction);

            float ResultSpeedModifider = SetSlopeSpeedModifiderOnAngle(GroundAngle);

            if (ResultSpeedModifider == 0f) return;
            
            //胶囊体高度减去射线距离 
            //ColliderCenterInLocalSpace.y和 localScale.y相乘是为了将碰撞体中心在本地空间中的高度转换成世界空间中的实际高度
            float DistanceToFloatingPoint = 
                stateMachine.playerRef.ColliderUtility.capsuleColliderData.ColliderCenterInLocalSpace.y *
                stateMachine.playerRef.transform.localScale.y - hit.distance;

            if (DistanceToFloatingPoint == 0f) return; //说明胶囊体底部正好在地面

            //需要减去GetPlayerVerticalVelocity().y防止出现刚体自身的力同时施加
            float AmountToLift = DistanceToFloatingPoint * slopeData.StepReachForce - GetPlayerVerticalVelocity().y; //漂浮多少
            
            Vector3 LiftAmount = new Vector3(0f,  AmountToLift, 0f);
            
            stateMachine.playerRef.RigidBody.AddForce(LiftAmount, ForceMode.VelocityChange);
        }
    }

    private float SetSlopeSpeedModifiderOnAngle(float Angle)
    {
        float ResultSlopeSpeedModifider = MovementData.SlopeSpeedAngle.Evaluate(Angle);
        stateMachine.ReusableData.MovementOnSlopeSpeedModifier = ResultSlopeSpeedModifider;

        return ResultSlopeSpeedModifider;
    }

    protected override void AddInputActionsCallback()
    {
        base.AddInputActionsCallback();

        //当移动按钮松开时
        stateMachine.playerRef.playerInput.PlayerActions.Movement.canceled += OnMovementCanceled;
        
        //翻滚
        stateMachine.playerRef.playerInput.PlayerActions.Dash.started += OnRollingStarted;
    }



    protected override void RemoveInputActionsCallback()
    {
        base.RemoveInputActionsCallback();
        
        stateMachine.playerRef.playerInput.PlayerActions.Movement.canceled -= OnMovementCanceled;
        
        stateMachine.playerRef.playerInput.PlayerActions.Dash.started -= OnRollingStarted;
    }
    
    protected virtual void OnMove()
    {
        if (stateMachine.ReusableData.bShouldWalk) //走路状态
        {
            stateMachine.ChangeState(stateMachine.WalkingState);
            return;
        }
        
        stateMachine.ChangeState(stateMachine.RunningState); 
    }
    
    /* 通用切换Idle状态 */
    protected virtual void OnMovementCanceled(InputAction.CallbackContext context)
    {
        stateMachine.ChangeState(stateMachine.IdleState);
    }
    
    protected virtual void OnRollingStarted(InputAction.CallbackContext context)
    {
        stateMachine.ChangeState(stateMachine.RollingState);
    }
}
