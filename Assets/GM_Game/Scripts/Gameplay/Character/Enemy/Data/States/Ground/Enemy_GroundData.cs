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
    
    public Transform CachedPlayerTransform { get; set; }
    
    public void InitializeAgent(NavMeshAgent agent)
    {
        if (agent == null) return;
        
        agent.speed = BaseSpeed;
        agent.angularSpeed = AngularSpeed;
        agent.acceleration = Acceleration;
        agent.stoppingDistance = StoppingDistance;
    }
}
