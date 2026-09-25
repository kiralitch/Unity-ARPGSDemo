using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

/*
 * 玩家移动状态
 * 
 */

public class Player_MovementStates : IState
{
    protected Player_MovementStateMachine stateMachine;
    
    //移动资产
    protected Player_GroundedData MovementData;
    
    public Player_MovementStates(Player_MovementStateMachine playerMovementStateMachine)
    {
        stateMachine = playerMovementStateMachine;
        MovementData = stateMachine.playerRef.AssetData.GroundedData;
        
        InitData();
    }

    private void InitData()
    {
        stateMachine.ReusableData.TimeToReachTargetRotation = MovementData.RotationData.TargetRotationReachTime;
    }

    public virtual void Enter()
    {
        Debug.Log("State" + GetType().Name);

        stateMachine.playerRef.AnimationClipData.SetRootTarget(1, 0, 0);
        if (stateMachine.playerRef.CurrentStateMode == PlayStateMode.Movement)
        {
            AddInputActionsCallback();
        }
        //stateMachine.playerRef.AnimationClipData.ResetAttackAnimationTime(0);
        //stateMachine.playerRef.AnimationClipData.ResetAttackAnimationTime(1);
    }
    
    public virtual void Exit()
    {
        RemoveInputActionsCallback();
    }

    public virtual void HandleInput()
    {
        ReadPlayerMovementInput();
    }

    public virtual void Update()
    {
        
    }

    public virtual void PhysicsUpdate() //FixUpdate() 在Update()之后执行
    {
        Move();
        
    }

    public virtual void OnAnimatationEnterEvent()
    {
       
    }

    public virtual void OnAnimatationExitEvent()
    {
        
    }

    public virtual void OnAnimationTransitionEvent()
    {
        
    }

    #region 主方法
    /* 读取玩家输入 */
    private void ReadPlayerMovementInput()
    {
        //从PlayerAction里面直接读取玩家是否由按键
        stateMachine.ReusableData.MovementInput = stateMachine.playerRef.playerInput.PlayerActions.Movement.ReadValue<Vector2>();
    }
    
    private void Move()
    {
        if(stateMachine.ReusableData.MovementInput == Vector2.zero || stateMachine.ReusableData.MovementSpeedModifier == 0f) return; //如果玩家输入为0，则不移动
        if(stateMachine.playerRef.CurrentStateMode == PlayStateMode.Combat) return;
        
        stateMachine.playerRef.AnimationClipData.SetRootTarget(1f, 0f, 0f);
        
        Vector3 MovementDirection = GetMovementInputDirection();

        float targetRotationYAngle = Rotate(MovementDirection);

        Vector3 targetRotationDirection = GetTargetRotationDirection(targetRotationYAngle);

        float Speed = GetSpeedMovement();
        
        //注意：AddForce方法是在刚体原有的力加上新的力，所以要减去原有的速度
        Vector3 currentPlayerHorizontalVel = GetPlayerHorizontalVelocity(); //获取刚体原有的速度
        
        stateMachine.playerRef.RigidBody.AddForce(
            targetRotationDirection * Speed - currentPlayerHorizontalVel,
            ForceMode.VelocityChange
            );
    }

    /* 根据给定的方向向量，计算出角色在 Y 轴上应该旋转到的目标角度 */
    private float Rotate(Vector3 Direction)
    {
        float direciontAngle = UpdateTargetRotation(Direction);

        RotateTowardTargerRoatation();
        
        return direciontAngle;
    }
    #endregion
    
    /* 可重写 */
    /* 设置根动画输入权重 */
    protected void SetAnimationInputWeight(PlayableType CurrentStateType, int AnimatinoIndex, float weight)
    {
        var animData = stateMachine.playerRef.AnimationClipData;
        if (CurrentStateType == PlayableType.Movement)
        {
            animData.SetMovementTarget(AnimatinoIndex, weight);
            animData.SetRootTarget(1f, 0f, 0f);
        }
    }

    protected Vector3 GetMovementInputDirection()
    {
        return new Vector3(stateMachine.ReusableData.MovementInput.x, 0f, stateMachine.ReusableData.MovementInput.y);
    }

    protected float GetSpeedMovement()
    {
        return MovementData.BaseSpeed * stateMachine.ReusableData.MovementSpeedModifier * stateMachine.ReusableData.MovementOnSlopeSpeedModifier;
    }
    
    protected Vector3 GetPlayerHorizontalVelocity()
    {
        Vector3 PlayerHorizontalVel = stateMachine.playerRef.RigidBody.velocity;
        PlayerHorizontalVel.y = 0f;
        return PlayerHorizontalVel;
    }

    protected float GetDirectionAngle(Vector3 direction)
    {
        float direciontAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        if (direciontAngle < 0f) direciontAngle += 360f; //若是-90，则变成270
        
        return direciontAngle;
    }

    /* 添加摄像机角度 比如角色90°往右走时摄像机也往右90°最后角色会变成180°旋转 */
    protected float AddCameraRotationToAngle(float angle)
    {
        //将摄像机的 Y 轴旋转角度叠加到计算出的相对角度上，得到世界空间中的目标绝对角度
        angle += stateMachine.playerRef.MainCameraTransform.eulerAngles.y;
        if (angle > 360f) angle -= 360f; //600会返回240

        return angle;
    }
    
    //将刚体朝向选装到摄像机角度
   protected void RotateTowardTargerRoatation()
   {
       //当前角色朝向角度
       float currentYAngle = stateMachine.playerRef.RigidBody.rotation.eulerAngles.y;

       if (currentYAngle == stateMachine.ReusableData.CurrentTargetRotatino.y) return;

       //平滑旋转角度
       float SmoothYAngle = Mathf.SmoothDampAngle(
           currentYAngle,
           stateMachine.ReusableData.CurrentTargetRotatino.y,
           ref stateMachine.ReusableData.DampedTargetRotationCurrentVelocity.y,
           stateMachine.ReusableData.TimeToReachTargetRotation.y - stateMachine.ReusableData.DampedTargetRotationPassedTime.y
           );

       stateMachine.ReusableData.DampedTargetRotationPassedTime.y += Time.deltaTime;

       Quaternion targetRotation = Quaternion.Euler(0f, SmoothYAngle, 0f);
       
       stateMachine.playerRef.RigidBody.MoveRotation(targetRotation);
   }
   
   private void UpdateTargetRotationData(float targetAngle)
   {
       stateMachine.ReusableData.CurrentTargetRotatino.y = targetAngle;
       stateMachine.ReusableData.DampedTargetRotationPassedTime.y = 0f; //重置已过去时间
   }

   /* 更新目标旋转值 */
   protected float UpdateTargetRotation(Vector3 Direction, bool bShouldConsiderCameraRoation = true)
   {
       float direciontAngle = GetDirectionAngle(Direction);

       if (bShouldConsiderCameraRoation)
       {
           direciontAngle = AddCameraRotationToAngle(direciontAngle);
       }

       if (direciontAngle != stateMachine.ReusableData.CurrentTargetRotatino.y)
       {
           UpdateTargetRotationData(direciontAngle);
       }

       return direciontAngle;
   }
   
   protected Vector3 GetTargetRotationDirection(float TargetAngle)
   {
       return Quaternion.Euler(0f, TargetAngle, 0f) * Vector3.forward;
   }

   /* 直接设置为0,防止出现停止后滑行 */
   protected void ResetVelocity()
   {
       stateMachine.playerRef.RigidBody.velocity = Vector3.zero;
   }

   protected Vector3 GetPlayerVerticalVelocity()
   {
       return new Vector3(0f, stateMachine.playerRef.RigidBody.velocity.y, 0f);
   }
   /* 玩家水平方向减速 */
   protected void DecelerateHorizontally()
   {
       Vector3 HorizontalVel = GetPlayerHorizontalVelocity();
       stateMachine.playerRef.RigidBody.AddForce(-HorizontalVel * stateMachine.ReusableData.MovementDecelerattionForce, ForceMode.Acceleration);
   }

   /* 通过对比玩家水平上速度的向量长度来判断是否在移动中（大于这个值就是正在移动） */
   protected bool IsMovingHorizontally(float limiter = 0.1f)
   {
       Vector2 PlayerHorizontalMovement = new Vector2(
           GetPlayerHorizontalVelocity().x,
           GetPlayerHorizontalVelocity().z
           );

       return PlayerHorizontalMovement.magnitude > limiter;
   }

   #region 输入函数
   /* 输入回调函数 */
   protected virtual void AddInputActionsCallback()
   {
       //绑定回调函数
       stateMachine.playerRef.playerInput.PlayerActions.WalkToggle.started += OnWalkToggleStarted;
   }

   protected virtual void RemoveInputActionsCallback()
   {
       //解绑
       stateMachine.playerRef.playerInput.PlayerActions.WalkToggle.started -= OnWalkToggleStarted;
   }
   
   /* 切换走路 */
   protected virtual void OnWalkToggleStarted(InputAction.CallbackContext context)
   {
       stateMachine.ReusableData.bShouldWalk = !stateMachine.ReusableData.bShouldWalk;
   }
   
   #endregion
}
