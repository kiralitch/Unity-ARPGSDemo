using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class StaminaBar_C : MonoBehaviour
{
    public Slider StaminaSlider;
    
    private float fadeDuration = 0.3f; // 淡入淡出时间
    
    private CanvasGroup canvasGroup;
    private Tween fadeTween;
    private float MaxStamina;
    private float CurrentStamina;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        CurrentStamina = MaxStamina;
    }

    public void SetPlayerMaxStamina(float MaxStamina)
    {
        this.MaxStamina = MaxStamina;
    }

    private void UpdateStaminaBar(float TargetValue)
    {
        StaminaSlider.value = TargetValue;
    }

    private void FixedUpdate()
    {
        HandleStaminaBarFade(IsStaminaFull());
    }

    #region 渐入渐出动画
    private bool IsStaminaFull()
    {
        return Mathf.Approximately(StaminaSlider.value, StaminaSlider.value = 1f);
    }

    private void HandleStaminaBarFade(bool bShouldHide)
    {
        fadeTween?.Kill();

        if (bShouldHide)
        {
            fadeTween = canvasGroup.DOFade(0f, fadeDuration).
                OnComplete(() => canvasGroup.interactable = false);
        }
        else
        {
            canvasGroup.interactable = true;
            fadeTween = canvasGroup.DOFade(1f, fadeDuration);
        }
    }

    #endregion
    

}
