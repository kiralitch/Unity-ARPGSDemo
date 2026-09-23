using System;
using UnityEngine;
using System.Collections.Generic;

[Serializable]
public class EnemyAbilityData
{
    [field:Header("基础信息")] 
    [field:SerializeField]
    public string skillName { get; private set; }
    
    [field: SerializeField][Tooltip("按照序列执行连招动画")]
    public List<AnimationClip> AttackClips { get; private set; }
    
    [field: SerializeField] 
    public float animationSpeed { get; private set; } = 1f;
    
    [field:SerializeField]
    public float AbilityDamage { get; private set; } = 10f;
    
    [field: SerializeField]
    public float AttackDistance { get; private set; }
}
