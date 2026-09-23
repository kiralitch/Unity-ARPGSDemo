using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

/*
 * 菜单UI管理类
 * 
 */

public class EscUIManager : SingleTon<EscUIManager>
{
    public Dictionary<string, string> pathDict; //路径配置字典
    public Dictionary<string, GameObject> prefabDict; //预制体缓存字典
    public Dictionary<string, BasePanel> panelDict; //已打开界面缓存字典
    
    private Transform _uiRoot; //UI根节点

    public Transform GetUIRoot
    {
        get
        {
            if (_uiRoot == null)
            {
                if (GameObject.Find("Canvas"))
                    _uiRoot = GameObject.Find("Canvas").transform;
                else
                    _uiRoot = new GameObject("Canvas").transform;
            };
            return _uiRoot;
        }
    }

    public EscUIManager()
    {
        InitDictionary();
    }

    private void InitDictionary()
    {
        prefabDict = new Dictionary<string, GameObject>();
        panelDict = new Dictionary<string, BasePanel>();

        pathDict = new Dictionary<string, string>()
        {
            { UIConst.PackagePanel, "Assets/GM_Game/Prefabs/UIPrefabs/HUDPrefabs/Package/PackagePanel.prefab" },
            
            
        };
    }

    /* 通过字符串获取界面 */
    public BasePanel GetPanel(string name)
    {
        BasePanel panel = null;
        //检测是否已经打开
        if (panelDict.TryGetValue(name, out panel))
        {
            return panel;
        }

        return null;
    }

    /* 异步打开面板，加载完成后通过回调返回 */
    public void OpenPanel(string name, Action<BasePanel> onOpened = null)
    {
        // 已打开则直接返回
        if (panelDict.TryGetValue(name, out var existing))
        {
            Debug.Log("界面已打开: " + name);
            onOpened?.Invoke(existing);
            return;
        }
        
        if (!pathDict.TryGetValue(name, out string location))
        {
            Debug.LogError($"未配置界面路径: {name}");
            onOpened?.Invoke(null);
            return;
        }

        // 如果预制体已缓存，则直接调用CreatePanel
        if (prefabDict.TryGetValue(name, out var cachedPrefab))
        {
            CreatePanel(name, cachedPrefab, onOpened);
            return;
        }

        // 通过YooAsset异步加载
        var handle = YooAssets.LoadAssetAsync<GameObject>(location);
        handle.Completed += (h) =>
        {
            if (h.Status == EOperationStatus.Succeed)
            {
                GameObject prefab = h.AssetObject as GameObject;
                if (!prefabDict.ContainsKey(name))
                    prefabDict.Add(name, prefab);
                CreatePanel(name, prefab, onOpened);
            }
            else
            {
                Debug.LogError($"加载 UI 失败::{name}, 地址::{location}");
                onOpened?.Invoke(null);
            }
        };
    }

    /* 加载完成后需要添加进字典里并打开界面 */
    private void CreatePanel(string name, GameObject prefab, Action<BasePanel> onOpened)
    {
        GameObject panelObj = GameObject.Instantiate(prefab, GetUIRoot); //实例化UI预制体，并将其挂到UI根节点下
        BasePanel panel = panelObj.GetComponent<BasePanel>();
        if (panel == null)
        {
            Debug.LogError($"预制体上缺少 BasePanel 组件::{name}");
            onOpened?.Invoke(null);
            return;
        }

        panelDict.Add(name, panel);
        panel.OpenPanel(name);
        onOpened?.Invoke(panel);
    }
    
    /* 关闭界面，并返回成功是否 */
    public bool ClosePanel(string name)
    {
        BasePanel panel = null;
        if (!panelDict.TryGetValue(name, out panel))
        {
            Debug.Log("界面未打开" + name);
            return false;
        }
        panel.ClosePanel();
        return true;
    }
}

/* UI唯一标识符 */
public class UIConst
{
    public const string PackagePanel = "PackagePanel";   
}
