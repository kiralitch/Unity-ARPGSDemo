using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_MovementState : IState
{
    protected Enemy_MovementStateMachine stateMachine;

    public Enemy_MovementState(Enemy_MovementStateMachine StateMachineRef)
    {
        stateMachine = StateMachineRef;
    }

    #region 状态接口

    public virtual void Enter()
    {
        Debug.Log($"Enemy:{stateMachine.EnemyRef.transform.name}当前状态：{GetType().Name}");
    }

    public virtual void Exit()
    {
        
    }

    public virtual void HandleInput()
    {
        
    }

    public virtual void Update()
    {
        
    }

    public virtual void PhysicsUpdate()
    {
        //攻击/受击流程进行中，移动状态机完全让位，不追击也不触发攻击
        if (IsCombatBusy()) return;

        if (stateMachine.EnemyRef.AIController.IsPlayerInAttackRange())
        {
            OnAttack();
        }
        else
        {
            OnMove();
        }
    }

    /* 战斗状态机（攻击、受击）是否正在接管角色 */
    protected bool IsCombatBusy()
    {
        if (stateMachine.EnemyRef.AIController.IsAttacking()) return true;

        var CombatMachine = stateMachine.EnemyRef.CombatStateMachine;

        return CombatMachine.GetCurrentState() == CombatMachine.AttackState;
    }

    /* 受击状态下移动状态机不参与任何表现控制 */
    protected bool IsHitReacting()
    {
        return stateMachine.GetCurrentState() == stateMachine.HitReactState;
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
    
    #endregion

    #region 主方法

    private void OnMove()
    {
        if (stateMachine.EnemyRef.AIController.GetBlackBoardValue("ChasePlayer"))
        {
            stateMachine.EnemyRef.AIController.AIOnMove(
                stateMachine.EnemyRef.CommonSO.GroundedData.CachedPlayerTransform
                );
        }
    }

    private void OnAttack()
    {
        if (!stateMachine.EnemyRef.AIController.GetBlackBoardValue("Attack"))
        {
            stateMachine.EnemyRef.AIController.SetBlackBoardValue("Attack", true);
        }
    }

    #endregion

    protected void SetPlayableMovementInput(int AnimatinoIndex, float weight)
    {
        var animData = stateMachine.EnemyRef.AnimationClipData;
        animData.SetMovementTarget(AnimatinoIndex, weight);

        /* 战斗接管期间不能把根权重抢回移动动画。
           攻击连招靠 Attack 权重播放，一旦这里把根权重设成 (1,0,0)，
           连招动画当帧被降为 0 权重，混合结果退回移动姿势，表现为抽搐 */
        if (IsCombatBusy()) return;

        animData.SetRootTarget(1f, 0f, 0f);
    }

}
