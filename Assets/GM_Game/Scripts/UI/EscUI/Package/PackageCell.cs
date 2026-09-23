using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YooAsset;

/*
 * 物品格子
 * 
 */

public class PackageCell : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    private Transform UIIcon;
    [SerializeField]
    private Transform UISelect;
    [SerializeField]
    private Transform UIText;
    [SerializeField]
    private Transform UIDelectSelect;
    [SerializeField]
    private Transform UIMouseSelect;
    
    //动态数据
    private PackageLocalItem packageLocalItem;
    //静态数据 
    private PackageItem PackageTableItem;
    //背包界面父节点
    private PackagePanel UIParent;

    //渐变动画 
    private Tween fadeTween;
    private CanvasGroup SelectGroup;
    private CanvasGroup MouseSelectGroup;
    
    [Range(0f, 0.5f)]
    public float fadeDuration = 0.3f; // 淡入淡出时间
    
    private void Awake()
    {
        UIDelectSelect.gameObject.SetActive(false);

        SelectGroup = UISelect.GetComponent<CanvasGroup>();
        MouseSelectGroup = UIMouseSelect.GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        SelectGroup.alpha = 0f;
        MouseSelectGroup.alpha = 0f;
    }

    /* 更新格子的子物件 */
    public void RefreshCell(PackageLocalItem packageLocalData, PackagePanel packagePanel)
    {
        //获取物品动态和静态数据
        packageLocalItem = packageLocalData;
        PackageTableItem = GameManager.GetInstance.GetPackageItemByID(packageLocalItem.id);
        UIParent = packagePanel;
        
        //物品名称
        UIText.GetComponent<Text>().text = PackageTableItem.name;

        //物品图标异步加载
        var handle = YooAssets.LoadAssetAsync<Texture2D>(PackageTableItem.ImagePath);
        handle.Completed += (h) =>
        {
            if (h.Status == EOperationStatus.Succeed)
            {
                SetAsyncLoadSprite(h);
            }
            else
            {
                Debug.Log($"{GetType().ToString()}物品图片加载失败");
            }
        };
    }

    //获取Texture2D后获取Sprite并设置到图标上
    private void SetAsyncLoadSprite(AssetOperationHandle handle)
    {
        Texture2D t = handle.AssetObject as Texture2D;
        Sprite temp = Sprite.Create(t, new Rect(0, 0, t.width, t.height), Vector2.zero);

        if (temp != null)
        {
            UIIcon.GetComponent<Image>().sprite = temp;
        }
    }

    #region 鼠标点击事件接口

    public void OnPointerClick(PointerEventData eventData)
    {
        if (UIParent.ChooseItemUID == packageLocalItem.uid) return; //说明已经在展示这个细节
        UIParent.ChooseItemUID = packageLocalItem.uid;
        UpdateSelectAni(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        UpdateMouseEnterAni(false);
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        UpdateMouseEnterAni(true);
    }
    
    public void SetLastSelectToZero(PackageLocalItem LastChooseItem)
    {
        if (LastChooseItem != packageLocalItem) return;
        
        UpdateSelectAni(true);
    }

    private void UpdateMouseEnterAni(bool bIsHide)
    {
        fadeTween?.Kill();
        if (!bIsHide)
        {
            MouseSelectGroup.interactable = true;
            fadeTween = MouseSelectGroup.DOFade(1f, fadeDuration);
        }
        else
        {
            fadeTween = MouseSelectGroup.DOFade(0f, fadeDuration).
                OnComplete(() => MouseSelectGroup.interactable = false);
        }
    }

    private void UpdateSelectAni(bool bIsHide)
    {
        fadeTween?.Kill();
        if (!bIsHide)
        {
            SelectGroup.interactable = true;
            fadeTween = SelectGroup.DOFade(1f, fadeDuration);
        }
        else
        {
            fadeTween = SelectGroup.DOFade(0f, fadeDuration).
                OnComplete(() => SelectGroup.interactable = false);
        }
    }

    #endregion
    

}
