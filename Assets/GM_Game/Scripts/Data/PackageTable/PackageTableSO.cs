using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Item/PackageTable", fileName = "PackageTable")]
public class PackageTableSO : ScriptableObject
{
    [field:SerializeField]
    public List<PackageItem> DataList { get; private set; }
}

[Serializable]
public class PackageItem
{
    public int ID;
    public int type;
    
    public string name;
    public string description;
    public string skillDescription; //详细描述 
    public string ImagePath; //图片路径
    public int num;
}
