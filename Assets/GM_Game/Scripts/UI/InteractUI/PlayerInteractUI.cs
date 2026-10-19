using DG.Tweening;
using TMPro;
using UnityEngine;

/*
 * 玩家交互UI
 * 
 */

public class PlayerInteractUI : MonoBehaviour
{
    private TMP_Text InteractText;
    private CanvasGroup canvasGroup;
    
    private float fadeDuration = 0.3f; // 淡入淡出时间
    private Tween fadeTween;
    
    public void Init()
    {
        InteractText = transform.GetComponentInChildren<TMP_Text>(true);
        canvasGroup = transform.GetComponentInChildren<CanvasGroup>(true);

        InitUI();
    }

    private void InitUI()
    {
        if (canvasGroup == null) return;

        canvasGroup.alpha = 0f;
        InteractUIView(false);
    }

    public void KillDoTween()
    {
        fadeTween?.Kill();
    }

    //true为显示，false为隐藏
    public void InteractUIView(bool bShouldView, string InterText = null)
    {
        KillDoTween();

        if (InterText != null && InteractText != null)
        {
            InteractText.SetText(InterText);
        }

        if (bShouldView)
        {
            //显示
            canvasGroup.interactable = true;
            fadeTween = canvasGroup.DOFade(1f, fadeDuration);
        }
        else
        {
            //隐藏
            fadeTween = canvasGroup.DOFade(0f, fadeDuration).
                OnComplete(() => canvasGroup.interactable = false);
        }
    }
}
