using System;
using System.Drawing;
using UnityEngine;
using UnityEngine.EventSystems;

/*
 * 背包菜单选择
 * 
 */

public class PackageMeusCell : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    private Transform UISelect;

    public bool bIsSelected;

    //背包界面父节点
    private PackagePanel UIParent;
    
    private void Awake()
    {
        bIsSelected = UISelect.gameObject.activeSelf;
    }

    public bool RefreshUI(PackagePanel parent)
    {
        if (UIParent == null)
        {
            UIParent = parent;
        }
        
        return bIsSelected;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (bIsSelected) return;
        
        //每次点击后需要传入菜单的孩子索引
        UIParent.RefreshMenuUI();
        UIParent.SetLastMenuSelectToFalse();
        SetSelectActive(true);
    }

    public void SetSelectActive(bool b)
    {
        UISelect.gameObject.SetActive(b);
        bIsSelected = b;
    }
}
