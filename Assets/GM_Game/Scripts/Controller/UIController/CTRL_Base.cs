using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * 跟controller关联的UIview
 * 
 */

public class CTRL_Base : IDisposable
{
    public CTRL_Base(UIBase view) {}
    
    public virtual void ShowView() {}
    public virtual void HideView() {}
    
    public void Dispose()
    {   
        
        
    }
}
