using UnityEngine;

public class CapsuleColliderData
{
    public CapsuleCollider Collider { get; private set; }
    public Vector3 ColliderCenterInLocalSpace { get; private set; }  //胶囊体中心在局部的空间

    public void Initialize(GameObject go)
    {
        if (Collider != null) return;
        
        Collider = go.GetComponent<CapsuleCollider>();
        UpdateColliderData();
        
    }

    //更新当前胶囊体的中心
   public void UpdateColliderData()
    {
        ColliderCenterInLocalSpace = Collider.center;
    }
}
