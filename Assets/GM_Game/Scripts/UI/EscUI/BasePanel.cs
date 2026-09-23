using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * 菜单UI通用Panel
 * 
 */

public class BasePanel : MonoBehaviour
{
    protected bool bIsRemove = false;
    protected new string name;

    protected virtual void Awake()
    {
        
    }

    public virtual void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }

    public virtual void OpenPanel(string name)
    {
        this.name = name;
        SetActive(true);
    }

    public virtual void ClosePanel()
    {
        bIsRemove = true;
        SetActive(false);
        Destroy(gameObject);

        if (EscUIManager.GetInstance.panelDict.ContainsKey(name))
        {
            EscUIManager.GetInstance.panelDict.Remove(name);
        }
    }
}
