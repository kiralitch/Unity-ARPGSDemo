using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class GM_Commad
{
    [MenuItem("CMCmd/打开背包界面")]
    public static void OpenPackagePanel()
    {
        EscUIManager.GetInstance.OpenPanel(UIConst.PackagePanel);
    }

    [MenuItem("CMCmd/添加物品")]
    public static void AddLocalPackage()
    {
        PackageLocalData.GetInstance.Items = new List<PackageLocalItem>();
        for (int i = 1; i < 4; i++)
        {
            PackageLocalItem localItem = new()
            {
                uid = Guid.NewGuid().ToString(),
                id = i,
                num = i,
            };
            
            PackageLocalData.GetInstance.Items.Add(localItem);
        }
        
        PackageLocalData.GetInstance.SavePackage();
    }

    [MenuItem("CMCmd/读取物品")]
    public static void ReadLocalPackage()
    {
        List<PackageLocalItem> items = PackageLocalData.GetInstance.LoadPackage();
        foreach (var packageLocalItem in items)
        {
            Debug.Log(packageLocalItem.ToString());
        }
    }
}
