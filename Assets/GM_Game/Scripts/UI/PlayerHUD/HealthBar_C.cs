using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar_C : MonoBehaviour
{
    public Image HealthBar;
    public Image CachedBar;
    
    private float MaxHealth;
    private float CurrentHealth;

    private Coroutine DelayCachedBarCoroutine;

    /* 设置血量上限 = 显式初始化
       一旦跳过，之后真正设进来的 100 就再也刷不满血条，表现为血条恒为 0 */
    public void SetMaxHealth(float maxHealth)
    {
        MaxHealth = maxHealth;
        CurrentHealth = MaxHealth;
        UpdateHealthBar(GetFillRate());
    }

    private void Start()
    {
        /* 若外部还没调用过 SetMaxHealth，这里用默认上限兜底
           调用过的话当前血量已被设置好，不要覆盖成 0 */
        if (MaxHealth <= 0f)
        {
            CurrentHealth = MaxHealth;
            UpdateHealthBar(GetFillRate());
        }
    }

    private void Update()
    {
        //if (Input.GetKey(KeyCode.S)) TakeDamage(1f);
    }

    public void TakeDamage(float Damage)
    {
        CurrentHealth = Mathf.Max(CurrentHealth - Damage, 0f); //减少生命
        UpdateHealthBar(GetFillRate());
    }

    /* 血量比例。MaxHealth 为 0（没有正确配置/初始化）时直接返回 0，
       避免 0/0=NaN 传进 Image.fillAmount 触发 “Invalid AABB” 渲染报错 */
    private float GetFillRate()
    {
        if (MaxHealth <= 0f) return 0f;
        return Mathf.Clamp01(CurrentHealth / MaxHealth);
    }

    private void UpdateHealthBar(float TargetFill)
    {
        HealthBar.fillAmount = TargetFill; //设置血条量（小数）
        
        if(DelayCachedBarCoroutine != null) StopCoroutine(DelayCachedBarCoroutine);

        DelayCachedBarCoroutine = StartCoroutine(DelayCachedBarUpdate(TargetFill));
    }

    private IEnumerator DelayCachedBarUpdate(float targetFill)
    {
        yield return new WaitForSeconds(0.2f); //每次更新等0，2秒后再更新

        float CurrentFill = CachedBar.fillAmount;

        for (float i = 0; i < 0.25f; i += Time.deltaTime)
        {
            //缓冲条在0.25s之内更新自己的值
            CachedBar.fillAmount = Mathf.Lerp(CurrentFill, targetFill, i / 0.25f);
            yield return null; //让出当前帧，等待下一帧继续循环
        }
        CachedBar.fillAmount = targetFill;
    }
}
