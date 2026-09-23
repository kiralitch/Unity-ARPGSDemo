using UnityEngine;

/*
 * 提示词管理
 * 管理所有Tips(因为是实例化所以不需要对象可以直接在其他函数调用)
 */

public class TipsManger : SingleTon<TipsManger>
{
    public void ShowSystemTips(string TipMsg)
    {
        ResourceManager.GetInstance.LoadPrefabAsync(
            "Assets/GM_Game/Prefabs/UIPrefabs/WidgetPrefabs/SystemTip",
            (GameObject go) =>
            {
                if (go == null) { return; }
                
                go.transform.SetParent(GameObject.Find("Canvas").transform); //使用GameObject.Find直接查找父类Canvas 
                go.transform.localPosition = new Vector3(0, 160); //初始位置
                go.transform.localScale = Vector2.one; //初始缩放

                SystemTips tips = go.GetComponent<SystemTips>();
                if (tips != null)
                {
                    tips.ReflashUI(TipMsg);
                }    
                
            }
        );
    }
}
