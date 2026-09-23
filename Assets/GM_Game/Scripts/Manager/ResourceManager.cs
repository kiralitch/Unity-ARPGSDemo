using System;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

/*
 * 资源加载函数库
 * 
 */

public class ResourceManager : SingleTon<ResourceManager>
{
    //缓存判断
    private Dictionary<string, AssetOperationHandle> PrefabDic = new Dictionary<string, AssetOperationHandle>();
    
    //callback:一个委托，接收一个GameObject参数 当预制体加载并实例化完成后，会调用这个委托，把实例化后的GameObject传给调用者
    public void LoadPrefabAsync(string Path, Action<GameObject> callback) //加载Prefab
    {
        if (PrefabDic.ContainsKey(Path))
        {
            //如果字典已有，则直接使用已有的参数
            callback?.Invoke(PrefabDic[Path].InstantiateSync());
        }
        else
        {
            Global.Instance.YooPackage.LoadAssetAsync(
                Path).Completed += (AssetOperationHandle handle) => //.Completed +=：资源加载的其中一个方法
            {
                GameObject ObjectPrefab = handle.InstantiateSync(); //返回一个实例化后的Prefab对象的GameObject

                if (!PrefabDic.ContainsKey(Path))
                {
                    PrefabDic.Add(Path, handle);
                }
            
                //使用C#的空条件运算符?.，如果调用者传递了有效的回调函数，就执行它，并把实例化后的GameObject作为参数传入
                //使用后调用者的回调函数会自带传入的数据
                callback?.Invoke(ObjectPrefab); 
            };
        }
    }
}
