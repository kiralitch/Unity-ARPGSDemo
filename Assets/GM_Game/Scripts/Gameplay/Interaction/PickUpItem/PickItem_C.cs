using UnityEngine;

public class PickItem_C : CommonInteracItem
{
    private void Awake()
    {
        Init();
    }

    protected override void Init()
    {
        if (InteractCollider == null)
        {
            InteractCollider = gameObject.GetComponentInChildren<Collider>(true);
        }
        
        base.Init();
    }
}
