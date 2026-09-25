using System;
using DG.Tweening;
using UnityEngine;

/*
 * 挂载在敌人身上的UIManager
 * 
 */

public class EnemyUIManager : MonoBehaviour
{
    public HealthBar_C HealthBar;

    [SerializeField]
    private float fadeDuration = 0.3f;
    
    private Tween fadeTween;
    private CanvasGroup HealthGroup;
    private Enemy EnemyRef;

    private void Awake()
    {
        EnemyRef = GetComponent<Enemy>();
        HealthBar.SetPlayerMaxHealth(EnemyRef.CommonAssetData.EnemyCommonData.MaxHealth);
        HealthGroup = HealthBar.GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        EnemyRef.CommonAssetData.EnemyCommonData.OnHealthChange += BindHealthBarChanged;
    }
    
    private void OnDisable()
    {
        EnemyRef.CommonAssetData.EnemyCommonData.OnHealthChange -= BindHealthBarChanged;
    }

    private void FixedUpdate()
    {
        HandleStaminaBarFade(IsHealthFull());
    }

    #region 谈出淡入动画

    private bool IsHealthFull()
    {
        return Mathf.Approximately(HealthBar.HealthBar.fillAmount, 1f);
    }

    private void HandleStaminaBarFade(bool bShouldHide)
    {
        fadeTween?.Kill();

        if (bShouldHide)
        {
            fadeTween = HealthGroup.DOFade(0f, fadeDuration).
                OnComplete(() => HealthGroup.interactable = false);
        }
        else
        {
            HealthGroup.interactable = true;
            fadeTween = HealthGroup.DOFade(1f, fadeDuration);
        }
    }

    #endregion
    
    #region 绑定事件函数

    private void BindHealthBarChanged(float Damage)
    {
        if (Damage <= 0f) return;
        HealthBar.TakeDamage(Damage);
    }

    #endregion
}
