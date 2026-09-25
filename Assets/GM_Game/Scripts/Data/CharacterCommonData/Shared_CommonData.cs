using System;
using UnityEngine;
using UnityEngine.Serialization;

/*
 * 游戏角色通用数据
 * 
 */

[Serializable]
public class Shared_CommonData
{
    [field:SerializeField]
    public float CurrentHealth { get; set; } = 100f;
    [field:SerializeField]
    public float MaxHealth { get; private set; } = 100f;
    
    [field: FormerlySerializedAs("<CommonDamageScale>k__BackingField")]
    [field:SerializeField]
    public float CommonDamageScales { get; private set; } = 1.2f;
    
    [field:SerializeField]
    public float CommonDefence { get; private set; } = 5f;
    
    public event Action<float> OnHealthChange; //绑定事件
    public event Action OnDeathCall;

    public virtual void InitData()
    {
        CurrentHealth = MaxHealth;
    }

    //设置生命同时发送广播
    public float SetCurrentHealthDamage
    {
        get => CurrentHealth;
        set
        {
            var CachedDamege = value;
            CachedDamege -= CommonDefence;
            
            CurrentHealth -= CachedDamege;
            OnHealthChange?.Invoke(CachedDamege); //UI改变

            if (CurrentHealth <= 0f)
            {
                OnDeathCall?.Invoke();
            }
        }
    }
}
