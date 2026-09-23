using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

/*
 * 通用武器碰撞箱 
 * 
 */

public class SharedWeaponHitBox : MonoBehaviour
{
    [Header("碰撞目标")]
    public string TargetTag = "Enemy";
    
    [SerializeField]
    private Transform Self;
    private CommonActor SelfRef;
    private float CachedDamage; //缓存伤害
    private Collider WeaponHitBox;
    private List<GameObject> HitTargets = new List<GameObject>();

    private void Awake()
    {
        if (Self == null)
        {
            Debug.Log("自身未赋值");
            return;
        }

        SelfRef = Self.GetComponent<CommonActor>();

        WeaponHitBox = GetComponent<Collider>();
        if (WeaponHitBox == null)
        {
            WeaponHitBox = gameObject.AddComponent<CapsuleCollider>(); //没有则添加
        }

        WeaponHitBox.isTrigger = true;
        WeaponHitBox.enabled = false;
    }

    public void EnableHitBox()
    {
        Debug.Log("开启");
        HitTargets.Clear(); //清空防止继续触发
        WeaponHitBox.enabled = true;
    }

    public void DisableHitBox()
    {
        Debug.Log("关闭");
        WeaponHitBox.enabled = false;
    }

    private void OnTriggerEnter(Collider HitTarget)
    {
        if (!HitTarget.CompareTag(TargetTag))
        {
            Debug.Log($"{HitTarget.tag}不是敌人");
            return;
        }

        if (HitTargets.Contains(HitTarget.gameObject)) return;
        
        HitTargets.Add(HitTarget.gameObject); //防止多次碰撞

        var DamegeInterface = HitTarget.GetComponent<IDamageable>();
        if (DamegeInterface != null && SelfRef != null)
        {
            DamegeInterface.TakeDamage(CachedDamage, SelfRef);
            Debug.Log($"命中目标 {HitTarget.name}，造成 {CachedDamage}点伤害");
        }
        else
        {
            //debug
            Debug.LogWarning($"目标 {HitTarget.name} 没有实现 IDamageable 接口");
        }
    }

    public void SetCachedDamage(float damage)
    {
        CachedDamage = damage;
    }
}
