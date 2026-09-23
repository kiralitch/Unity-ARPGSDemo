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

    public void SetPlayerMaxHealth(float maxHealth)
    {
        MaxHealth = maxHealth;
    }
    
    private void Start()
    {
        CurrentHealth = MaxHealth;
        UpdateHealthBar(CurrentHealth / MaxHealth);
    }

    private void Update()
    {
        //if (Input.GetKey(KeyCode.S)) TakeDamage(1f);
    }

    public void TakeDamage(float Damage)
    {
        CurrentHealth = Mathf.Max(CurrentHealth - Damage, 0f); //减少生命
        float TargetFill = CurrentHealth / MaxHealth;
        UpdateHealthBar(TargetFill);
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
