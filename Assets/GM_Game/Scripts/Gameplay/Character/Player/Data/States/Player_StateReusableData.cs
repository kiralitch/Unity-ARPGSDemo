using UnityEngine;

public class Player_StateReusableData
{
    public Vector2 MovementInput { get; set; }
    public float MovementSpeedModifier { get; set; } = 1f;
    public float MovementOnSlopeSpeedModifier { get; set; } = 1f;
    
    public float MovementDecelerattionForce { get; set; } = 1f; //减速力量
    public bool bShouldWalk { get; set; }
    
    /* 移动函数缓存数据 */
    private Vector3 currentTargetRotatino;
    private Vector3 timeToReachTargetRotation;
    private Vector3 dampedTargetRotationCurrentVelocity;
    private Vector3 dampedTargetRotationPassedTime;
    
    /* 攻击函数缓存数据 */
    private Vector3 currentAttackTargetRotation;
    private Vector3 attackTimeToReachTargetRotation;
    private Vector3 attackDampedTargetRotationCurrentVelocity;
    private Vector3 attackDampedTargetRotationPassedTime;

    public Vector3 LastMeshPosition { get; set; }

    //需要引用才能获取Vector3的x,y,z
    public ref Vector3 CurrentTargetRotatino 
    {
        get
        {
            return ref currentTargetRotatino;
        }
    }
    
    public ref Vector3 TimeToReachTargetRotation 
    {
        get
        {
            return ref timeToReachTargetRotation;
        }
    }

    public ref Vector3 DampedTargetRotationCurrentVelocity
    {
        get
        {
            return ref dampedTargetRotationCurrentVelocity;
        }
    }

    public ref Vector3 DampedTargetRotationPassedTime
    {
        get
        {
            return ref dampedTargetRotationPassedTime;
        }
    }

    public ref Vector3 CurrentAttackTargetRotation
    {
        get
        {
            return ref currentAttackTargetRotation;
        }
    }
    
    public ref Vector3 AttackTimeToReachTargetRotation
    {
        get
        {
            return ref attackTimeToReachTargetRotation;
        }
    }
    
    public ref Vector3 AttackDampedTargetRotationCurrentVelocity
    {
        get
        {
            return ref attackDampedTargetRotationCurrentVelocity;
        }
    }
    
    public ref Vector3 AttackDampedTargetRotationPassedTime
    {
        get
        {
            return ref attackDampedTargetRotationPassedTime;
        }
    }
}
