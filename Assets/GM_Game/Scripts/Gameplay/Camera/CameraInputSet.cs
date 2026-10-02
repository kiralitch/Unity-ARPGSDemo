using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;


public class CameraInputSet : MonoBehaviour
{
    /* 最初使用了inputProvider.XYAxis.name来存入字典，
     但是这样无法生效，因为在已经将inputProvider.XYAxis设置为null了后
     字典的TryGetValue如果中继续使用inputProvider.XYAxis.name
     会发生无法找到这个inputRef的name
     */
    public string XYAxis = "XY";
    public string ZAxis = "Z";
    
    private CinemachineInputProvider inputProvider;

    //缓存输入
    private Dictionary<string, InputActionReference> CacheInputReference = new Dictionary<string, InputActionReference>();
    
    private void Awake()
    {
        inputProvider = GetComponent<CinemachineInputProvider>();
        
        if (inputProvider != null)
        {
            CacheInputReference[XYAxis] = inputProvider.XYAxis;
            CacheInputReference[ZAxis] = inputProvider.ZAxis;
        }
    }

    #region 主函数

    public void SetWhenLockInCamera(bool bIsLockIn)
    {
        bool bInputIsNull = inputProvider.XYAxis == null;

        if (bIsLockIn)
        {
            if (bInputIsNull) return;

            inputProvider.XYAxis = null;
        }
        else
        {
            if (!bInputIsNull) return;

            if (!CacheInputReference.TryGetValue(
                    XYAxis,
                    out var InputRef)) return;

            inputProvider.XYAxis = InputRef;
        }
    }
    
    //TODO 所有摄像机输入全部失效或生效
    public void SetAllInput(bool bVisible)
    {
        if (inputProvider == null) return;
        if (inputProvider.enabled != bVisible) inputProvider.enabled = bVisible;
    }


    #endregion
    

}
