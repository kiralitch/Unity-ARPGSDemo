

/*
 * 敌人通用
 * 
 */

using System;
using UnityEngine;

[Serializable]
public class Enemy_CommonData : Shared_CommonData
{
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
