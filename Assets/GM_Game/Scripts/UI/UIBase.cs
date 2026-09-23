using UnityEngine;

/*
 * UI基类
 * 适用于所有UI
 */

public class UIBase : MonoBehaviour
{
    public virtual void InitView() {}

    public virtual void Show(bool isShow = true)
    {
        gameObject.SetActive(isShow); //设置激活状态
    }
    
}
