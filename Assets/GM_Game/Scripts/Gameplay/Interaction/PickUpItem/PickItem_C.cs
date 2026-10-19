using System;
using System.Collections.Generic;
using UnityEngine;

/*
 * 可捡起物品通用类
 * 
 */

public class PickItem_C : CommonInteracItem
{
    public List<Items> StoreItems;

    private PlayerInteractUI UIParent;
    private string CacheInteractText;
    
    private void Awake()
    {
        Init();
    }

    protected override void Init()
    {
        if (InteractCollider == null)
        {
            InteractCollider = gameObject.GetComponent<Collider>();
        }

        UIParent = gameObject.GetComponentInChildren<PlayerInteractUI>(true);

        if (UIParent != null)
        {
            UIParent.Init();
        }

        base.Init();
    }

    protected override void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            Debug.Log("不是玩家进入");
            return;
        }
        
        //玩家可交互
        PlayerInteract(true, other);
        
        //TODO UI显示
        if (UIParent != null)
        {
            UIParent.InteractUIView(true, CacheInteractText);
        }
    }

    protected override void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            Debug.Log("不是玩家退出");
            return;
        }
        
        //玩家不可交互
        PlayerInteract(false, other);
        
        //TODO UI消失
        if (UIParent != null)
        {
            UIParent.InteractUIView(false, null);
            CacheInteractText = null;
        }
    }

    //玩家交互
    private void PlayerInteract(bool b, Collider other)
    {
        //获取接口
        var IInteract = other.GetComponent<IInteractable>();

        //玩家是否可交互
        if (IInteract == null) return;
        IInteract.BindInteractInput(b, out CacheInteractText);
        
        if (b)
        {
            IInteract.Interact(this);
            Debug.Log("该交互物品已经传入");
        }
        else
        {
            IInteract.ClearCachedItem();
            Debug.Log("传入物品已清除");
        }
    }

    public override void Interact()
    {
        PickUp();
    }

    /* 添加物品 */
    private void PickUp()
    {
        if (StoreItems == null || StoreItems.Count == 0) return;
        
        Debug.Log("开始捡起");

        //拿到背包动态数据（LoadPackage 返回并缓存的就是 PackageLocalData.Items，直接改它）
        List<PackageLocalItem> LocalItems = PackageLocalData.GetInstance.LoadPackage();

        foreach (var item in StoreItems)
        {
            if (item == null) continue;
            if (item.number <= 0) continue;

            //相同 ID 的物品堆叠到已有格子上，不重复生成新格子
            PackageLocalItem localItem = LocalItems.Find(x => x.id == item.ItemID);
            if (localItem == null)
            {
                localItem = new PackageLocalItem
                {
                    uid = Guid.NewGuid().ToString(), //实例唯一 ID，用于背包格子选中/详情查询
                    id = item.ItemID,
                    num = item.number,
                };
                LocalItems.Add(localItem);
            }

            localItem.num += item.number;
        }

        //写回本地存档
        PackageLocalData.GetInstance.SavePackage();

        //拾取后清空掉落物，避免重复拾取；与退出交互范围时清空缓存保持一致
        StoreItems.Clear();
        if (Player.Instance != null)
        {
            Player.Instance.ClearCachedItem();
        }

        //TODO 回对象池或销毁（当前保留原物品对象，仅清空掉落内容）
        UIParent.KillDoTween();
        Destroy(gameObject);
    }
}

[Serializable]
public class Items
{
    public int ItemID;
    public int number;
}