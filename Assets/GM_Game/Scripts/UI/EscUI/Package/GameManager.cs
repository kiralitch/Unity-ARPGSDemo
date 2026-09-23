using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

/*
 * 用来打开UI菜单界面的通用类
 * 
 */

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    private static object InstanceLock = new object(); //线程同步的锁对象

    private PackageTableSO PackageTable; //静态数据处理
    
    public static GameManager GetInstance
    {
        get
        {
            if (instance == null)
            {
                lock (InstanceLock) //检查后加锁，防止多线程同时访问
                {
                    if (instance == null) {  instance = new GameManager(); }
                }
            }
            
            return instance;
        }
    }

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        //打开游戏自动打开背包界面
        StartCoroutine(WaitOpenPanel());
        //EscUIManager.GetInstance.OpenPanel(UIConst.PackagePanel);
    }

    private IEnumerator WaitOpenPanel()
    {
        yield return new WaitForSeconds(2f);
        PackageTable = GetPackageTable();
        yield return new WaitForSeconds(0.5f);
        EscUIManager.GetInstance.OpenPanel(UIConst.PackagePanel);
    }

    private void Update()
    {
        /*if (Input.GetKey(KeyCode.Space))
        {
            //打开游戏自动打开背包界面
            EscUIManager.GetInstance.OpenPanel(UIConst.PackagePanel);
        }*/
    }

    /* 读取静态数据 */
    public PackageTableSO GetPackageTable()
    {
        if (PackageTable != null) return PackageTable;
        
        var handle = YooAssets.LoadAssetAsync<PackageTableSO>("Assets/GM_Game/ScriptableObject/PackageTable/PackageTableSO.asset");
        handle.Completed += (h) =>
        {
            if (h.Status == EOperationStatus.Succeed)
            {
                PackageTable = h.AssetObject as PackageTableSO;
            }
            else
            {
                Debug.Log("静态物品加载失败");
            }
        };

        return PackageTable;
    }

    /* 读取动态数据 */
    public List<PackageLocalItem> GetPackageLocalData()
    {
        return PackageLocalData.GetInstance.LoadPackage();
    }
    
    /* 根据ID找到物品 */
    public PackageItem GetPackageItemByID(int id)
    {
        List<PackageItem> packageDataList = PackageTable.DataList;
        foreach (var Item in packageDataList)
        {
            if(!Item.ID.Equals(id)) continue;
            return Item;
        }
        return null;
    }
    
    /* 根据UID获取本地动态数据 */
    public PackageLocalItem GetPackageLocalItemByUID(string uid)
    {
        List<PackageLocalItem> LocalList = GetPackageLocalData();
        foreach (var LocalItem in LocalList)
        {
            if(!LocalItem.uid.Equals(uid)) continue;
            return LocalItem;
        }

        return null;
    }
    
    /* 获取排序好的本地背包物品,按照id大小排序 */
    public List<PackageLocalItem> GetSortPackageLocalData()
    {
        List<PackageLocalItem> Local = PackageLocalData.GetInstance.LoadPackage();
        Local.Sort(new PackageComparer()); //自定义比较器 PackageComparer 对列表进行排序
        return Local;
    }
}

public class PackageComparer : IComparer<PackageLocalItem>
{
    public int Compare(PackageLocalItem x, PackageLocalItem y)
    {
        /*
        返回 负数：x 排在 y 前面
        返回 0：x 和 y 相等，顺序不变
        返回 正数：x 排在 y 后面
        */
        
        return y.id.CompareTo(x.id);
    }
}
