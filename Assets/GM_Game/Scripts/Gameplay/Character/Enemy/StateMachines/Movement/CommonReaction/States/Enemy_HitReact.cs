using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy_HitReact : Enemy_GroundedState
{
    private CommonActor ExecutorRef;

    /* 本次受击锁定下来的后退方向与朝向（受击瞬间决定，之后不再跟着攻击者变） */
    private Vector3 HitBackDirection;
    private Vector3 FacingDirection;

    /* 受击瞬间记录的攻击者位置 */
    private Vector3 LockedExecutorPosition;

    private bool bHasLockedTarget = false;

    /* 后退位移已经完成，不再做任何表现控制 */
    private bool bKnockbackFinished = false;

    /* 后退剩余的位移量（米）与剩余时间，用来推进速度曲线 */
    private float RemainingKnockbackDistance;
    private float RemainingKnockbackTime;

    private Rigidbody CachedRigidBody;
    private bool bHasCachedRigidBody;

    /* 是否有一根受击期间被我们改成非 kinematic 的刚体需要在退出时还原 */
    private bool bRestoredRigidBodyKinematic;

    /* 后退期间刚体原来的重力设置，退出时还原 */
    private bool CachedUseGravity;

    private Enemy_GroundData GroundedData => stateMachine.EnemyRef.CommonSO.GroundedData;

    public Enemy_HitReact(Enemy_MovementStateMachine StateMachineRef) : base(StateMachineRef)
    {

    }

    public override void Enter()
    {
        base.Enter();

        /* 受击期间不再追击/攻击，这里停掉 Agent，否则受击动画会推着敌人继续滑向玩家。
           Agent 停住之后它也就不会再写 transform，位置可以安全地交给受击位移控制 */
        StopAgentMovement();
        CheckPlayerPositon();

        stateMachine.EnemyRef.AnimationClipData.ResetCommonReactionAnimationTime(0);
        stateMachine.EnemyRef.AnimationClipData.SetCommonReacitonTarget(0);
        stateMachine.EnemyRef.AnimationClipData.SetRootTarget(0, 0, 1);
    }

    /* 受击瞬间锁定后退方向与朝向，并把后退量算成初速度交给刚体 */
    private void CheckPlayerPositon()
    {
        bHasLockedTarget = false;
        bKnockbackFinished = false;

        HitBackDirection = Vector3.zero;
        FacingDirection = Vector3.zero;
        RemainingKnockbackDistance = 0f;
        RemainingKnockbackTime = 0f;

        if (ExecutorRef == null) return;

        LockedExecutorPosition = ExecutorRef.transform.position;

        Vector3 BackDir = stateMachine.EnemyRef.transform.position - LockedExecutorPosition;
        BackDir.y = 0f;

        /* 敌人与攻击者位置完全重合时没有有效的后退方向，
           此时只做朝向，不做位移，避免沿 NaN 方向把敌人推飞 */
        if (BackDir.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning($"{stateMachine.EnemyRef.name} 与攻击者 {ExecutorRef.name} 位置重合，本次受击不产生后退位移");
            return;
        }

        HitBackDirection = BackDir.normalized;

        /* 正对着攻击者，也就是背对着后退方向 */
        FacingDirection = -HitBackDirection;

        bHasLockedTarget = true;

        RemainingKnockbackDistance = GroundedData.HitKnockbackDistance;
        RemainingKnockbackTime = GroundedData.HitKnockbackDuration;

        TryStartRigidbodyKnockback();
    }

    #region 刚体后退

    /* 受击后退优先交给刚体：敌人平时是 kinematic，完全由 Agent 写位置，
       这里临时解除 kinematic 才能用速度推动 */
    private void TryStartRigidbodyKnockback()
    {
        if (!GroundedData.bUseRigidbodyKnockback) return;
        
        if (stateMachine.EnemyRef.Body == null) return;

        CachedUseGravity = stateMachine.EnemyRef.Body.useGravity;

        if (stateMachine.EnemyRef.Body.isKinematic)
        {
            stateMachine.EnemyRef.Body.isKinematic = false;
            bRestoredRigidBodyKinematic = true;
        }

        /* 受击期间不要重力：敌人高度由地面贴合逻辑负责，
           交给重力会让胶囊体缓慢下沉到地面以下 */
        stateMachine.EnemyRef.Body.useGravity = false;

        /* 清掉残留速度，保证后退初速度完全由本次受击决定 */
        stateMachine.EnemyRef.Body.velocity = Vector3.zero;
        stateMachine.EnemyRef.Body.angularVelocity = Vector3.zero;

        stateMachine.EnemyRef.Body.AddForce(HitBackDirection * GroundedData.HitKnockbackSpeed, ForceMode.VelocityChange);

        Debug.Log($"{stateMachine.EnemyRef.name} 受击后退：方向 {HitBackDirection}，初速度 {GroundedData.HitKnockbackSpeed}");
    }

    /* 刚体后退：每帧施加反向加速度把速度衰减到 0，
       这样前后两帧速度都由代码给定，实际位移不会被物理解算结果带偏 */
    private void PhysicsUpdateRigidbodyKnockback()
    {
        if (stateMachine.EnemyRef.Body == null || stateMachine.EnemyRef.Body.isKinematic) return;

        Vector3 HorizontalVelocity = stateMachine.EnemyRef.Body.velocity;
        HorizontalVelocity.y = 0f;

        /* 速度方向与后退方向相反说明位移已经超出目标距离（例如被撞墙弹回），
           继续衰减只会把它推得更远，直接停住 */
        if (Vector3.Dot(HorizontalVelocity, HitBackDirection) <= 0f)
        {
            stateMachine.EnemyRef.Body.velocity = Vector3.zero;
            return;
        }

        stateMachine.EnemyRef.Body.AddForce(-HitBackDirection * GroundedData.HitKnockbackDrag, ForceMode.Acceleration);
    }

    #endregion

    #region 运动学后退

    /* 刚体被禁用或不允许脱离 kinematic 时的兜底：按「速度曲线 × 时间」逐帧推 transform，
       推完再用 Agent.Warp 把 NavMesh 上的位置同步过去 */
    private void UpdateKinematicKnockback(float DeltaTime)
    {
        /* Duration 执行过程中被改小（例如 Inspector 里调参）时按当前值收口，
           避免剩余时间大于总时长导致速度曲线反向 */
        if (RemainingKnockbackTime > GroundedData.HitKnockbackDuration)
        {
            RemainingKnockbackTime = GroundedData.HitKnockbackDuration;
        }

        float Progress = GroundedData.HitKnockbackDuration - RemainingKnockbackTime;
        float Speed = GroundedData.HitKnockbackSpeed * (1f - Progress / GroundedData.HitKnockbackDuration);
        float Step = Mathf.Min(Speed * DeltaTime, RemainingKnockbackDistance);

        if (Step > 0f)
        {
            Vector3 NewPosition = stateMachine.EnemyRef.transform.position + HitBackDirection * Step;

            /* 走 NavMesh 采样而不是直接赋位置：墙面和台阶会被挡住，
               敌人不会因为受击被推进墙里或者掉出导航网格 */
            if (NavMesh.SamplePosition(NewPosition, out NavMeshHit Hit, 0.5f, NavMesh.AllAreas))
            {
                NewPosition = Hit.position;
            }

            stateMachine.EnemyRef.transform.position = NewPosition;
        }

        RemainingKnockbackDistance -= Step;
        RemainingKnockbackTime -= DeltaTime;
    }

    #endregion

    /* 受击状态覆盖基类的 PhysicsUpdate：基类会在玩家进入攻击范围时触发攻击，
       受击动画播完立刻接一段攻击，表现上就是「每次受击后都打一下」。
       受击期间完全不参与追击与攻击判定 */
    public override void PhysicsUpdate()
    {
        if (!bHasLockedTarget) return;

        RotateTowardsExecutor();

        UpdateKnockback();
    }

    /* 每次物理帧都朝受击瞬间锁定的攻击者位置转，转到目标朝向之后不再写旋转 */
    private void RotateTowardsExecutor()
    {
        if (Vector3.Angle(stateMachine.EnemyRef.RotationTransform.forward, FacingDirection) <= 1f) return;

        stateMachine.EnemyRef.RotateTowards(FacingDirection, GroundedData.HitFacingReachTime);
    }

    private void UpdateKnockback()
    {
        if (bKnockbackFinished) return;
        
        /* 刚体可用时由物理引擎推进位移，动画只负责姿势 */
        if (stateMachine.EnemyRef.Body != null && !stateMachine.EnemyRef.Body.isKinematic)
        {
            PhysicsUpdateRigidbodyKnockback();
            return;
        }

        if (RemainingKnockbackTime <= 0f) return;

        UpdateKinematicKnockback(Time.fixedDeltaTime);

        if (RemainingKnockbackDistance > 0f && RemainingKnockbackTime > 0f) return;

        bKnockbackFinished = true;
        WarpAgentToCurrentPosition();
    }

    public override void Exit()
    {
        base.Exit();

        StopKnockback();
        ResumeAgentMovement();
    }

    /* 退出受击：清速度、把刚体恢复成 kinematic 与原来的重力设置，
       位置交接给 Agent（Warp 到当前位置再恢复寻路，避免恢复控制时弹回受击前的位置） */
    private void StopKnockback()
    {
        RestoreRigidBody();

        bHasLockedTarget = false;
        bKnockbackFinished = true;
        RemainingKnockbackDistance = 0f;
        RemainingKnockbackTime = 0f;

        stateMachine.EnemyRef.ResetRotationVelocity();
    }

    public override void OnAnimationTransitionEvent()
    {
        base.OnAnimationTransitionEvent();

        stateMachine.ChangeState(stateMachine.IdleState);
    }

    /* 停住 NavMeshAgent，受击位移完全由受击状态与动画负责 */
    private void StopAgentMovement()
    {
        var Agent = stateMachine.EnemyRef.AIController?.Agent;
        if (Agent == null || !Agent.enabled) return;

        if (Agent.isOnNavMesh) Agent.ResetPath();

        Agent.updateRotation = false;
        Agent.isStopped = true;
    }

    /* 恢复 Agent 的常规控制 */
    private void ResumeAgentMovement()
    {
        var Agent = stateMachine.EnemyRef.AIController?.Agent;
        if (Agent == null || !Agent.enabled) return;

        /* 受击期间位置被推走了，恢复寻路之前必须先把 Agent 同步到新位置，
           否则它会按旧位置把敌人拉回受击前的地方 */
        WarpAgentToCurrentPosition();

        Agent.isStopped = false;
        Agent.updateRotation = true;
    }

    /* 把 Agent 瞬移到敌人当前所在的导航网格点。Warp 是唯一安全的外部改位方式，
       直接写 transform 会让 Agent 内部位置与 transform 不一致 */
    private void WarpAgentToCurrentPosition()
    {
        var Agent = stateMachine.EnemyRef.AIController?.Agent;
        if (Agent == null || !Agent.enabled || !Agent.isOnNavMesh) return;

        if (!UnityEngine.AI.NavMesh.SamplePosition(stateMachine.EnemyRef.transform.position, out UnityEngine.AI.NavMeshHit Hit, 1f, NavMesh.AllAreas)) return;

        Agent.Warp(Hit.position);
    }

    /* 还原受击期间对刚体做的改动，避免影响之后 Agent 驱动的常规移动 */
    private void RestoreRigidBody()
    {
        if (stateMachine.EnemyRef.Body == null) return;

        stateMachine.EnemyRef.Body.velocity = Vector3.zero;
        stateMachine.EnemyRef.Body.angularVelocity = Vector3.zero;
        stateMachine.EnemyRef.Body.useGravity = CachedUseGravity;

        if (bRestoredRigidBodyKinematic)
        {
            stateMachine.EnemyRef.Body.isKinematic = true;
            bRestoredRigidBodyKinematic = false;
        }
    }

    public void SetExecutorRef(CommonActor Executor)
    {
        if (Executor == null) return;
        ExecutorRef = Executor;
    }
}
