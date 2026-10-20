using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Input = UnityEngine.Windows.Input;

/*
 * 玩家通用攻击状态
 * 
 */

public class Player_AttackState : Player_CombatCommonState
{
    /*private float maxSpeed = 10f;
    private float attackAcceleration = 20f; // 加速度*/

    private float AttackDashDuration = 0.15f; //前冲总时长
    private float AttackDashSpeed = 20f; //前冲速度峰值
    private float AttackDashTimer; //剩余前冲时间
    private float AttackDashSpeedCached; //本次前冲速度(撞墙时可缩短)

    private const float AttackRayDistance = 0.6f; //前冲检测距离
    private const float AttackMoveSpeedMin = 1f; //前冲结束后的残留速度

    public Player_AttackState(Player_MovementStateMachine playerMovementStateMachine, Player_CombatStateMachine playerCombatStateMachine) : base(playerMovementStateMachine, playerCombatStateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        //停止移动
        CombatStateMachine.playerRef.RigidBody.velocity = Vector3.zero;

        MoveToTarget();
        //本次实际播放的连招段索引：必须在 PlayAttackAnimaiton 里下标自增之前取，
        //否则时间轴会一直停留在第 0 段，第二、三段会错误地执行第一段的事件
        int playedSegment = PlayAttackAnimaiton();

        //绑定本技能并进入本次播放的那一段，攻击开始传入所有的技能设置
        var Ticker = CombatStateMachine.playerRef.SkillTimeline;
        if (Ticker != null && CachedAbilitiesData != null)
        {
            Ticker.BeginAttack(
                CachedAbilitiesData.Timeline,
                CachedAbilitiesData.AttackClips.Count,
                CachedAbilitiesData.AbilityDamage,
                CachedAbilitiesData.animationSpeed,
                playedSegment,
                GetClipLengths(CachedAbilitiesData.AttackClips));
        }
    }
    
    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        UpdateAttackDash();

        //推进本段动画时间，到点执行音效/特效/伤害（不参与状态切换）
        var Ticker = CombatStateMachine.playerRef.SkillTimeline;
        if (Ticker != null)
        {
            Ticker.OnAttackFrame(Time.fixedDeltaTime);
        }
    }

    public override void Exit()
    {
        base.Exit();

        AttackDashTimer = 0f;

        //强制收尾并解绑，避免命中盒跨段残留
        CombatStateMachine.playerRef.SkillTimeline?.EndAttack();
    }

    #region Main

    /* 播放本次攻击动画，返回本次实际播放的连招段索引（供时间轴选择对应轨道） */
    private int PlayAttackAnimaiton()
    {
        if (CachedAbilitiesData == null || CachedAbilitiesData.AttackClips == null || CachedAbilitiesData.AttackClips.Count == 0)
        {
            Debug.LogError("攻击动画为空");
            return 0;
        }
        
        //需要先判断，不然index会一直加
        if (CombotIndex < 0 || CombotIndex >= CachedAbilitiesData.AttackClips.Count)
            CombotIndex = 0;

        int playedSegment = CombotIndex;
        
        CombatStateMachine.playerRef.AnimationClipData.PlayAttackClip(
            CachedAbilitiesData.AttackClips[CombotIndex],
            CachedAbilitiesData.AttackEndClips[CombotIndex],
            CombatStateMachine.playerRef.AnimationGraph
            );

        if (CombotIndex < CachedAbilitiesData.AttackClips.Count - 1)
        {
            CombotIndex++;
        }
        else
        {
            CombotIndex = 0;
        }

        return playedSegment;
    }

    /* 每段连招动画的时长（秒），时间轴据此把事件时间钳制在本段内 */
    private static float[] GetClipLengths(System.Collections.Generic.List<AnimationClip> clips)
    {
        if (clips == null) return null;

        var lengths = new float[clips.Count];
        for (int i = 0; i < clips.Count; i++)
        {
            lengths[i] = clips[i] != null ? clips[i].length : 0f;
        }
        return lengths;
    }

    /* 攻击朝向目标 */
    private void MoveToTarget()
    {
        float CameraYAngle = CombatStateMachine.playerRef.MainCameraTransform.eulerAngles.y;
        
        Rotate(CameraYAngle);

        //缓存本次前冲距离，前冲开始时一次性算完，中途不再读取技能数据
        AttackDashSpeedCached = CachedAbilitiesData.AttackDistance;
        AttackDashTimer = AttackDashDuration;

        UpdateAttackDash();
    }

    /* 前冲位移，速度由峰值衰减到0，接近瞬移的手感 */
    private void UpdateAttackDash()
    {
        if (AttackDashTimer <= 0f) return;

        AttackDashTimer -= Time.fixedDeltaTime;

        float Speed = Mathf.Lerp(
            AttackMoveSpeedMin,
            AttackDashSpeedCached,
            Mathf.Clamp01(AttackDashTimer / AttackDashDuration) //从1衰减到0
            );

        Vector3 Direction = GetAttackDashDirection(AttackDashSpeedCached);
        Quaternion targetRotation = Quaternion.Euler(0f, GetDirectionAngle(Direction), 0f);

        CombatStateMachine.playerRef.RigidBody.MoveRotation(targetRotation);
        CombatStateMachine.playerRef.RigidBody.MovePosition(
            CombatStateMachine.playerRef.RigidBody.position + Direction * (Speed * Time.fixedDeltaTime)
            );
        
        RotationAttackToTarget();
    }

    /* 算出本次前冲的实际方向，碰到墙或者悬崖先缩短距离 */
    private Vector3 GetAttackDashDirection(float Distance)
    {
        Vector3 Direction = GetTargetRotationDirection(CombatStateMachine.ReusableData.CurrentAttackTargetRotation.y);
        Direction.y = 0f;
        Direction.Normalize();

        return Direction * GetAttackDashDistance(Direction, Distance);
    }

    /* 前方有墙或者脚下没地面时缩短前冲距离，防止穿墙和飞出去 */
    private float GetAttackDashDistance(Vector3 Direction, float Distance)
    {
        float RayDistance = Mathf.Max(AttackRayDistance, Distance);

        if (Physics.Raycast(GetPlayerHeadPosition(), Direction, out RaycastHit hit, RayDistance, PlayerRef.LayerData.GroundLayer, QueryTriggerInteraction.Ignore))
        {
            Distance = Mathf.Min(Distance, Mathf.Max(hit.distance - AttackMoveSpeedMin, 0f));
        }
        else if (!Physics.Raycast(GetPlayerHeadPosition() + Direction * Distance, Vector3.down, out _, Distance + AttackRayDistance, PlayerRef.LayerData.GroundLayer, QueryTriggerInteraction.Ignore))
        {
            //前方落点没有地面
            Distance = 0f;
        }

        return Distance;
    }

    /* 胶囊体顶部往下一点，避免射线起点埋在墙里 */
    private Vector3 GetPlayerHeadPosition()
    {
        return PlayerRef.transform.position + Vector3.up * AttackRayDistance;
    }

    private float GetDirectionAngle(Vector3 direction)
    {
        float direciontAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        if (direciontAngle < 0f) direciontAngle += 360f;

        return direciontAngle;
    }

    private Player PlayerRef => CombatStateMachine.playerRef;

    /* 角色刚体原有的速度 */
    private Vector3 GetPlayerHorizontalVelocity()
    {
        Vector3 PlayerHorizontalVel = MovementStateMachine.playerRef.RigidBody.velocity;
        PlayerHorizontalVel.y = 0f;
        return PlayerHorizontalVel;
    }

    private float GetAttackMoveSpeed()
    {
        return CachedAbilitiesData.AttackDistance;
    }

    protected Vector3 GetTargetRotationDirection(float TargetAngle)
    {
        return Quaternion.Euler(0f, TargetAngle, 0f) * Vector3.forward;
    }
    
    private void Rotate(float Angle)
    {
        //更新缓存数据
        UpdateDataAngle(Angle);
        
        RotationAttackToTarget();
    }

    private void UpdateDataAngle(float Angle)
    {
        CombatStateMachine.ReusableData.CurrentAttackTargetRotation.y = Angle;
        MovementStateMachine.ReusableData.CurrentTargetRotatino.y = Angle;
        CombatStateMachine.ReusableData.AttackDampedTargetRotationPassedTime.y = 0f;
    }

    /* 角色朝向目标 */
    private void RotationAttackToTarget()
    {
        //当前角色朝向角度
        float currentYAngle = CombatStateMachine.playerRef.RigidBody.rotation.eulerAngles.y;
        
        //平滑旋转角度
        float SmoothYAngle = Mathf.SmoothDampAngle(
            currentYAngle,
            CombatStateMachine.ReusableData.CurrentAttackTargetRotation.y,
            ref CombatStateMachine.ReusableData.AttackDampedTargetRotationCurrentVelocity.y,
            CombatStateMachine.ReusableData.AttackTimeToReachTargetRotation.y - CombatStateMachine.ReusableData.AttackDampedTargetRotationPassedTime.y
        );

        CombatStateMachine.ReusableData.AttackDampedTargetRotationPassedTime.y += Time.deltaTime;

        Quaternion targetRotation = Quaternion.Euler(0f, SmoothYAngle, 0f);
       
        MovementStateMachine.playerRef.RigidBody.MoveRotation(targetRotation);
    }

    #endregion
    
    public override void OnAnimatationExitEvent()
    {
        CombatStateMachine.playerRef.PlayerWeaponHitBox.DisableHitBox();
    }

    public override void OnAnimatationEnterEvent()
    {
        CombatStateMachine.playerRef.PlayerWeaponHitBox.EnableHitBox();
    }

    public override void OnAnimationTransitionEvent()
    {
        //CombatStateMachine.playerRef.AnimationClipData.SetRootTarget(0,1, 0);
        CombatStateMachine.ChangeState(CombatStateMachine.AttackEndState);
    }
}
