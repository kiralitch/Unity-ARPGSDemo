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
    
    public Player_AttackState(Player_MovementStateMachine playerMovementStateMachine, Player_CombatStateMachine playerCombatStateMachine) : base(playerMovementStateMachine, playerCombatStateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        //停止移动
        CombatStateMachine.playerRef.RigidBody.velocity = Vector3.zero;

        MoveToTarget();
        PlayAttackAnimaiton();
    }
    
    #region Main

    private void PlayAttackAnimaiton()
    {
        if (CachedAbilitiesData == null || CachedAbilitiesData.AttackClips == null || CachedAbilitiesData.AttackClips.Count == 0)
        {
            Debug.LogError("攻击动画为空");
            return;
        }
        
        //需要先判断，不然index会一直加
        if (CombotIndex < 0 || CombotIndex >= CachedAbilitiesData.AttackClips.Count)
            CombotIndex = 0;
        
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
    }

    /* 攻击朝向目标 */
    private void MoveToTarget()
    {
        float CameraYAngle = CombatStateMachine.playerRef.MainCameraTransform.eulerAngles.y;
        
        Rotate(CameraYAngle);

        Vector3 targetRotationDirection = GetTargetRotationDirection(CameraYAngle);
        Vector3 horizontalVel = GetPlayerHorizontalVelocity();

        Vector3 NewPosition = new Vector3(GetAttackMoveSpeed(), 0f, GetAttackMoveSpeed());
        
        CombatStateMachine.playerRef.RigidBody.AddForce(
            targetRotationDirection * GetAttackMoveSpeed() - horizontalVel,
            ForceMode.Impulse
            );
        /*CombatStateMachine.playerRef.RigidBody.MovePosition(
            CombatStateMachine.playerRef.RigidBody.position + NewPosition
            );*/
    }

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
        CombatStateMachine.playerRef.AnimationClipData.SetRootTarget(0,1, 0);
        CombatStateMachine.ChangeState(CombatStateMachine.AttackEndState);
    }
}
