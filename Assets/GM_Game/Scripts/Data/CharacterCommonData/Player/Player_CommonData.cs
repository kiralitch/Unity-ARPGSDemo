using System;
using UnityEngine;

/*
 * 玩家基础数据
 * 
 */

[Serializable]
public class Player_CommonData : Shared_CommonData
{
    [field:SerializeField]
    public float CurrentStamina { get; private set; } = 30f;
    [field:SerializeField]
    public float MaxStamina { get; private set; } = 30f;
    
    public event Action<float> OnStaminaChanged; //绑定事件

    public override void InitData()
    {
        base.InitData();

        CurrentStamina = MaxStamina;
    }

    public float SetCurrentStamina
    {
        get => CurrentHealth;
        set { CurrentHealth -= value; OnStaminaChanged?.Invoke(value); }
    }
}
