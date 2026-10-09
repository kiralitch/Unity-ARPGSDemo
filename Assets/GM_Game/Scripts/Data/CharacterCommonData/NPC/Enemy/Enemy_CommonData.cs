

/*
 * 敌人通用
 * 
 */

using System;
using UnityEngine;

[Serializable]
public class Enemy_CommonData : Shared_CommonData
{
    /* 怪物 ID：用于任务系统按 targetId 计数击杀。
       在敌人通用数据资产（Enemy_CommonSO）上配置，同种怪物保持相同 ID */
    [field:SerializeField]
    public int MonsterId { get; set; } = 0;

    [field:SerializeField]
    public float Posture { get; private set; } = 80f;
    [field:SerializeField]
    public float MaxPosture { get; private set; } = 80f;
    
    public event Action<float> OnPostureChanged; //绑定事件

    public override void InitData()
    {
        base.InitData();

        Posture = MaxPosture;
    }
}
