using System;
using UnityEngine;
using UnityEngine.AI;

[Serializable]
public class Enemy_GroundData
{
    [field:SerializeField] [field:Range(0f,10f)]
    public float BaseSpeed { get; private set; } = 2f;

    [field: SerializeField]
    [field: Range(0f, 360f)]
    public float AngularSpeed { get; private set; } = 360f;

    [field: SerializeField]
    [field: Range(0f, 100f)]
    public float Acceleration { get; private set; } = 20f;
    
    [field: SerializeField]
    [field: Range(0f, 1f)]
    public float StoppingDistance { get; private set; } = 0.3f;

    [field:Header("视野设置")]
    [field: SerializeField]
    public float sightRange { get; private set; } = 10f;          // 视野半径
    [field: SerializeField]
    public float sightAngle { get; private set; } = 120f;         // 视野全角（度）
    [field: SerializeField]
    public LayerMask obstacleMask { get; private set; }          // 障碍物层（用于遮挡检测）

    [field:Header("攻击朝向设置")]
    [field: SerializeField] [field:Range(0.01f, 1f)]
    public float AttackRotationReachTime { get; private set; } = 0.15f; //攻击前旋转到玩家的耗时（越小转得越快）

    [field: SerializeField] [field:Range(0f, 10f)]
    public float AttackRange { get; private set; } = 2f; //进入该距离内即可切换攻击状态

    [field:Header("受击后退设置")]
    [field: SerializeField] [field:Range(0f, 10f)]
    public float HitKnockbackDistance { get; private set; } = 1.5f; //受击后要后退的距离（米）

    [field: SerializeField] [field:Range(0.01f, 3f)]
    public float HitKnockbackDuration { get; private set; } = 0.3f; //后退这段距离耗用的时间（秒），越小冲得越猛

    [field: SerializeField] [field:Range(0.01f, 1f)]
    public float HitFacingReachTime { get; private set; } = 0.1f; //受击后转向攻击者的耗时（越小转得越快）

    /* 用刚体冲击力实现后退，默认关闭。
       本项目场景的地面与障碍物用的是非凸 MeshCollider，非凸网格不会和动态刚体产生接触：
       刚体一旦脱离 kinematic 就会陷进地面，求解器按穿透反推，结果净位移为 0。
       要用刚体路径，需要先把这些 MeshCollider 勾上 Convex（或换成 Box/凸碰撞体） */
    [field: SerializeField]
    public bool bUseRigidbodyKnockback { get; private set; } = false;

    /* 刚体后退专用的减速（米/秒²），仅在 bUseRigidbodyKnockback 打开时生效；
       运动学路径的减速由「距离 / 时间」的速度曲线决定，不用这个值 */
    [field: SerializeField] [field:Range(0f, 30f)]
    public float HitKnockbackDrag { get; private set; } = 12f;

    public Transform CachedPlayerTransform { get; set; }

    /* 按「距离 / 时间」反推初速度，让角色在忽略阻力时正好滑出 Distance 米 */
    public float HitKnockbackSpeed => HitKnockbackDistance / Mathf.Max(HitKnockbackDuration, 0.01f);
    
    public void InitializeAgent(NavMeshAgent agent)
    {
        if (agent == null) return;
        
        agent.speed = BaseSpeed;
        agent.angularSpeed = AngularSpeed;
        agent.acceleration = Acceleration;
        agent.stoppingDistance = StoppingDistance;
    }
}
