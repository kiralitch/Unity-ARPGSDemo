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

    private bool bIsBeenHit = false;
    private float HealthBarViewTimer = 0f;
    private Tween fadeTween;
    private CanvasGroup HealthGroup;
    private Enemy EnemyRef;
    private Enemy_CommonData EnemyData;

    private void Awake()
    {
        EnemyRef = GetComponent<Enemy>();
        EnemyData = EnemyRef.CommonAssetData.EnemyCommonData;
        
        HealthBar.SetPlayerMaxHealth(EnemyRef.CommonAssetData.EnemyCommonData.MaxHealth);
        HealthGroup = HealthBar.GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        EnemyData.OnHealthChange += BindHealthBarChanged;
    }
    
    private void OnDisable()
    {
        EnemyData.OnHealthChange -= BindHealthBarChanged;

        /* 敌人死亡销毁时，血条 CanvasGroup 会随 GameObject 一起被销毁。
           DOTween 的安全模式（DOTweenSettings 里 useSafeMode = 1）会捕获到
           「目标已销毁但补间还在跑」并报错，所以在目标被销毁前必须先手动 Kill。
           同时清掉补间引用，避免残留的 Tween 对象继续持有已销毁的目标 */
        fadeTween?.Kill();
        fadeTween = null;

        bIsBeenHit = false;
    }

    private void Update()
    {
        UIHealthBarView(Time.deltaTime);
    }

    private void UIHealthBarView(float deltaTime)
    {
        if (!bIsBeenHit) return;

        if(bIsBeenHit)
            HandleHealthBarFade(false);
        
        HealthBarViewTimer += deltaTime;

        if (HealthBarViewTimer > 10f)
        {
            HandleHealthBarFade(true);
            HealthBarViewTimer = 0f;
            bIsBeenHit = false;
        }
    }

    #region 谈出淡入动画

    private bool IsHealthFull()
    {
        return Mathf.Approximately(HealthBar.HealthBar.fillAmount, 1f);
    }

    private void HandleHealthBarFade(bool bShouldHide)
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
        bIsBeenHit = true;
    }

    #endregion
}
