using System;
using UnityEngine;

/*
 * 挂在玩家身上的
 * 
 */

public class HUDManager : MonoBehaviour
{
    public HealthBar_C HealthBar;
    public StaminaBar_C StamaniBar;

    private Player PlayerRef;

    private void Awake()
    {
        PlayerRef = GetComponent<Player>();
        HealthBar.SetPlayerMaxHealth(PlayerRef.CommonAssetData.commonData.MaxHealth);
        StamaniBar.SetPlayerMaxStamina(PlayerRef.CommonAssetData.commonData.MaxStamina);
    }

    private void OnEnable()
    {
        PlayerRef.CommonAssetData.commonData.OnHealthChange += BindHealthBarChanged;
    }

    private void OnDisable()
    {
        PlayerRef.CommonAssetData.commonData.OnHealthChange -= BindHealthBarChanged;
    }

    #region 绑定事件函数

    private void BindHealthBarChanged(float TargetValue)
    {
        if (TargetValue <= 0) return;
        HealthBar.TakeDamage(TargetValue);
    }

    #endregion
    
}
