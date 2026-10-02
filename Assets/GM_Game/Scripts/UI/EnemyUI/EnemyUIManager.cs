using System;
using DG.Tweening;
using UnityEngine;

/*
 * 挂载在敌人身上的UIManager
 * 
 */

public class EnemyUIManager : MonoBehaviour
{
    [SerializeField]
    private float fadeDuration = 0.3f;

    [SerializeField]
    private float HealthBarShowDuration = 10f;

    private bool bIsBeenHit = false;
    private bool bHealthBarVisible = false;
    private float HealthBarViewTimer = 0f;
    private Tween fadeTween;
    private CanvasGroup HealthGroup;
    private Enemy EnemyRef;
    private HealthBar_C HealthBar;

    /* 敌人实例自己那份运行时血量数据 */
    private Enemy_CommonData EnemyData
    {
        get
        {
            if (EnemyRef == null) EnemyRef = GetComponent<Enemy>();
            return EnemyRef != null ? EnemyRef.RuntimeCommonData : null;
        }
    }

    /* 取本敌人自己的血条，只在第一次调用时从子层级查找并缓存 */
    private HealthBar_C ResolvedHealthBar
    {
        get
        {
            if (HealthBar == null)
            {
                HealthBar = GetComponentInChildren<HealthBar_C>(true);
            }
            return HealthBar;
        }
    }

    private void Awake()
    {
        EnemyRef = GetComponent<Enemy>();

        /* 血条挂在敌人自己身下，这里在自己的层级里解析，保证拿到的是本实例的血条 */
        HealthBar = GetComponentInChildren<HealthBar_C>(true);
    }
    
    private void Start()
    {
        HealthBar_C bar = ResolvedHealthBar;
        Enemy_CommonData data = EnemyData;

        if (bar == null || data == null)
        {
            Debug.LogWarning($"[EnemyUI] {name} 缺少 HealthBar 或 RuntimeCommonData，血条不可用");
            return;
        }

        bar.SetMaxHealth(data.MaxHealth);
        HealthGroup = bar.GetComponent<CanvasGroup>();

        data.OnHealthChange += BindHealthBarChanged;
    }

    private void OnDisable()
    {
        /* 只有成功订阅过才解绑，避免给 null 解绑 */
        Enemy_CommonData data = EnemyData;
        if (data != null)
        {
            data.OnHealthChange -= BindHealthBarChanged;
        }

        fadeTween?.Kill();
        fadeTween = null;

        bIsBeenHit = false;
        bHealthBarVisible = false;
    }

    private void Update()
    {
        UIHealthBarView(Time.deltaTime);
    }

    private void UIHealthBarView(float deltaTime)
    {
        if (!bIsBeenHit) return;

        /* 只在「刚被打中」的那一帧触发一次淡入。
           如果每帧都调 HandleHealthBarFade(false)，函数内部会先 Kill 掉上一帧
           刚创建的补间再重新建一个，补间永远停留在起始 alpha 无法推进到 1，
           血条看起来就一直不显示 */
        if (!bHealthBarVisible)
        {
            bHealthBarVisible = true;
            HandleHealthBarFade(false);
        }

        HealthBarViewTimer += deltaTime;

        /* 10 秒内不再受击就淡出隐藏 */
        if (HealthBarViewTimer > HealthBarShowDuration)
        {
            HandleHealthBarFade(true);
            HealthBarViewTimer = 0f;
            bIsBeenHit = false;
            bHealthBarVisible = false;
        }
    }

    #region 谈出淡入动画

    private void HandleHealthBarFade(bool bShouldHide)
    {
        if (HealthGroup == null) return;

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

        HealthBar_C bar = ResolvedHealthBar;
        if (bar == null) return;

        bar.TakeDamage(Damage);
        bIsBeenHit = true;
    }

    #endregion
}
