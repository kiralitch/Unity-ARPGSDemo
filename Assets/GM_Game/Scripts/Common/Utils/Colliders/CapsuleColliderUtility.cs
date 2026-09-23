using System;
using UnityEngine;

/*
 * 胶囊体逻辑
 * 
 */

[Serializable]
public class CapsuleColliderUtility
{
    public CapsuleColliderData capsuleColliderData { get; private set; } //获取胶囊碰撞体引用
    
    [field:SerializeField]
    public DefautColliderData  colliderData { get; private set; } //设置胶囊体数据
    [field: SerializeField] 
    public SlopeData slopeData { get; private set; } //可浮动数据

    public void InitColliderData(GameObject go)
    {
        if(capsuleColliderData != null) return;
        capsuleColliderData = new CapsuleColliderData();
        
        capsuleColliderData.Initialize(go);
    }

    /* 计算胶囊体空间 */
    public void CalculateCapsuleColliderDimensions()
    {
        SetCapsuleColliderRadius(colliderData.Radius);
        SetCapsuleColliderHeight(colliderData.Height * (1f - slopeData.StepHeightPercentage)); //高度以百分比为单位

        RecalculateCapsuleColliderCenter();

        // 胶囊体高度必须大于等于半径的2倍
        float HalfColliderHeright = capsuleColliderData.Collider.height / 2f;
        if (HalfColliderHeright < capsuleColliderData.Collider.radius)
        {
            SetCapsuleColliderRadius(HalfColliderHeright);
        }
        
        capsuleColliderData.UpdateColliderData();
    }

    /* 将默认高度减去当前高度后除以2 */
    public void RecalculateCapsuleColliderCenter()
    {
        float HeightDiffierent = colliderData.Height - capsuleColliderData.Collider.height;
        Vector3 NewColliderCenter = new Vector3(0f, colliderData.CenterY + (HeightDiffierent / 2f), 0f);
        capsuleColliderData.Collider.center = NewColliderCenter;
    }

    /* 设置胶囊体半径 */
    public void SetCapsuleColliderRadius(float radius)
    {
        capsuleColliderData.Collider.radius = radius;
    }
    
    /* 设置胶囊体高度 传入的值减去台阶高度的百分比就是最后设置的胶囊体结果 */
    public void SetCapsuleColliderHeight(float height)
    {
        capsuleColliderData.Collider.height = height;
    }
}
