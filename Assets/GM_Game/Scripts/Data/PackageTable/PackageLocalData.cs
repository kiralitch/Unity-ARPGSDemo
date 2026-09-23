using System;
using System.Collections.Generic;
using UnityEngine;

/*
 * 背包Json文件本地存档
 * 
 */

public class PackageLocalData : SingleTon<PackageLocalData>
{
    public List<PackageLocalItem> Items;

    /* 保存背包 */
    public void SavePackage()
    {
        string inventoryJson = JsonUtility.ToJson(Items);
        PlayerPrefs.SetString("PackageLocalData", inventoryJson);
        PlayerPrefs.Save();
    }

    /* 读取玩家存档背包 */
    public List<PackageLocalItem> LoadPackage()
    {
        if (Items != null) return Items; //Items不为空说明之前已经读取过了

        if (PlayerPrefs.HasKey("PackageLocalData"))
        {
            string InventoryJson = PlayerPrefs.GetString("PackageLocalData");
            List<PackageLocalItem> localData = JsonUtility.FromJson<List<PackageLocalItem>>(InventoryJson); //反序列化获取存储好的PackageLocalData
            return localData;
        }

        return new List<PackageLocalItem>();
    }
}

/* 本地物品存档 */
[Serializable]
public class PackageLocalItem
{
    public string uid;  //获取静态数据的id

    public int id;
    public int num;

    public override string ToString()
    {
        return string.Format("[id]:{0}, num:{1}", id, num);
    }
}