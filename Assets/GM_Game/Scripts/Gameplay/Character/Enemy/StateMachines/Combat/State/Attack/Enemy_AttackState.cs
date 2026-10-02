using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy_AttackState : Enemy_CombatCommonState
{
    /* 本次攻击使用的技能数据，由 Enemy 在切换状态前赋值 */
    public EnemyAbilityData CachedAbilityData;

    /* 进入攻击时锁定的玩家位置（世界坐标），之后不再跟随玩家移动 */
    private Vector3 LockedPlayerPosition;

    /* 是否成功锁定了玩家位置，没锁到就不做旋转 */
    private bool bHasLockedTarget;

    /* 锁定的朝向（已去掉 y 轴的水平方向） */
    private Vector3 LockedAttackDirection;

    /* 攻击前旋转的耗时 */
    private float AttackRotationReachTime = 0.15f;

    /* 已经结束攻击，避免同一段动画的过渡帧事件重复触发收尾逻辑 */
    private bool bAttackEnded;

    public Enemy_AttackState(Enemy_MovementStateMachine movementStateMachine, Enemy_CombatStateMachine combatStateMachine) : base(movementStateMachine, combatStateMachine)
    {
        AttackRotationReachTime = combatStateMachine.EnemyRef.CommonSO.GroundedData.AttackRotationReachTime;
    }

    #region Main

    public override void Enter()
    {
        base.Enter();

        bAttackEnded = false;

        StopAgentForAttack();
        LockPlayerPositionOnAttack();
        PlayAttackAnimation();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        
        RotateToLockedPosition();
    }

    public override void Exit()
    {
        base.Exit();

        bHasLockedTarget = false;
        bAttackEnded = false;
        CachedEnemy.ResetRotationVelocity(); //清掉角速度，避免下一次攻击起步抖动
        ResumeAgentAfterAttack();
    }

    /* 攻击期间停住 NavMeshAgent：否则 Agent 的转向和 RotateTowards 会同时写 transform.rotation，
       两个角速度叠加会让敌人抖动，而且 Agent 补间的位置更新会和攻击位移冲突 */
    private void StopAgentForAttack()
    {
        if (CachedEnemy.AIController == null) return;

        NavMeshAgent Agent = CachedEnemy.AIController.Agent;
        if (Agent == null || !Agent.enabled) return;

        if (Agent.isOnNavMesh) Agent.ResetPath();

        Agent.updateRotation = false; //朝向交给 RotateTowards 单独控制
        Agent.isStopped = true;
    }

    /* 恢复 Agent 的常规控制，攻击结束由移动状态接管移动与转向 */
    private void ResumeAgentAfterAttack()
    {
        if (CachedEnemy.AIController == null) return;

        NavMeshAgent Agent = CachedEnemy.AIController.Agent;
        if (Agent == null || !Agent.enabled) return;

        Agent.isStopped = false;
        Agent.updateRotation = true;
    }

    /* 播放本次技能的第一段连招，并把技能伤害写入碰撞箱 */
    private void PlayAttackAnimation()
    {
        if (CachedAbilityData == null)
        {
            Debug.LogError("进入攻击状态但没有技能数据，无法播放连招动画！");
            return;
        }

        CachedClipsData.PlayEnemyComboClip(
            CachedAbilityData,
            CachedEnemy.AnimationGraph
            );

        CachedEnemy.HitBoxUtils.SetCachedDamage(CachedClipsData.GetEnemyAbilityDamage());
    }

    /* 攻击前获取玩家位置：这里记录的是进入攻击瞬间的位置，玩家之后移动不会改变本次攻击的朝向 */
    private void LockPlayerPositionOnAttack()
    {
        bHasLockedTarget = false;
        LockedAttackDirection = Vector3.zero;

        if (CachedEnemy.AIController == null) return;
        if (!CachedEnemy.AIController.TryGetPlayerPosition(out LockedPlayerPosition)) return;

        Vector3 Direction = LockedPlayerPosition - CachedEnemy.transform.position;
        Direction.y = 0f;

        //敌人与玩家位置重合时没有有效朝向，直接跳过旋转
        if (Direction.sqrMagnitude < 0.0001f) return;

        LockedAttackDirection = Direction.normalized;
        bHasLockedTarget = true;
    }

    /* 每帧朝锁定的位置平滑旋转，直到朝向玩家为止 */
    private void RotateToLockedPosition()
    {
        if (!bHasLockedTarget) return;

        CachedEnemy.RotateTowards(LockedAttackDirection, AttackRotationReachTime);
    }

    /* 当前是否已经大致朝向锁定的玩家位置 */
    public bool IsFacingLockedTarget()
    {
        if (!bHasLockedTarget) return true;

        return Vector3.Angle(CachedEnemy.RotationTransform.forward, LockedAttackDirection) <= 5f;
    }

    public override void OnAnimatationEnterEvent()
    {
        base.OnAnimatationEnterEvent();
        
        CombatStateMachine.EnemyRef.HitBoxUtils.EnableHitBox();
    }

    public override void OnAnimatationExitEvent()
    {
        base.OnAnimatationExitEvent();
        
        CombatStateMachine.EnemyRef.HitBoxUtils.DisableHitBox();
    }

    /* 连招段落上的过渡帧触发：还有下一段就接着播，已经是最后一段就退出攻击
     * 注意：这个事件来自攻击动画，不是移动状态机的事件 */
    public override void OnAnimationTransitionEvent()
    {
        base.OnAnimationTransitionEvent();

        //已经收尾过就不再响应，避免同一段动画的事件重复触发收尾逻辑
        if (bAttackEnded) return;

        if (!CachedClipsData.PlayNextEnemyComboClip(CachedEnemy.AnimationGraph))
        {
            bAttackEnded = true;
            EndAttack();
        }
    }

    /* 连招播完，收尾并交还控制权 */
    private void EndAttack()
    {
        CachedEnemy.ResetRotationVelocity();
        CachedEnemy.OnAttackFinished();
    }

    #endregion
}
