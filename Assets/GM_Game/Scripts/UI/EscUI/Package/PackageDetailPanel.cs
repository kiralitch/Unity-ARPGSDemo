using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YooAsset;

/*
 * 细节面板
 * 
 */

public class PackageDetailPanel : MonoBehaviour
{
    [SerializeField]
    private Transform UIDesctiption;
    [SerializeField]
    private Transform UIIcon;
    [SerializeField]
    private Transform UITitle;
    [SerializeField]
    private Transform UISkillDesctiption;
    
    //动态数据
    private PackageLocalItem packageLocalItem;
    //静态数据 
    private PackageItem PackageTableItem;
    //背包界面父节点
    private PackagePanel UIParent;

    /* 刷新细节面板的UI */
    public void RefreshDetailPanel(PackageLocalItem localData, PackagePanel UIRoot)
    {
        packageLocalItem = localData;
        PackageTableItem = GameManager.GetInstance.GetPackageItemByID(packageLocalItem.id);
        UIParent = UIRoot;

        UITitle.GetComponent<Text>().text = PackageTableItem.name;
        UIDesctiption.GetComponent<Text>().text = PackageTableItem.description;
        UISkillDesctiption.GetComponent<Text>().text = PackageTableItem.skillDescription;
        
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

    private void SetAsyncLoadSprite(AssetOperationHandle Handle)
    {
        Texture2D t = Handle.AssetObject as Texture2D;
        Sprite temp = Sprite.Create(t, new Rect(0, 0, t.width, t.height), Vector2.zero);

        if (temp != null)
        {
            UIIcon.GetComponent<Image>().sprite = temp;
        }
    }
}
