using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public class AbilitiesData 
{
    [field:Header("基础信息")] 
    [field:SerializeField]
    public string skillName { get; private set; }
    [field: SerializeField][Tooltip("如果添加了多个动画则自动检测为连段动作")]
    public List<AnimationClip> AttackClips { get; private set; }
    [field: SerializeField]
    public List<AnimationClip> AttackEndClips { get; private set; }

    [field: SerializeField] 
    public float animationSpeed { get; private set; } = 1f;
    
    [field:SerializeField]
    public float AbilityDamage { get; private set; } = 10f;

    [field: SerializeField]
    [Tooltip("拖拽 InputAction 资产到此处")]
    public InputActionReference inputAction { get; private set; }

    [field: SerializeField]
    public float AttackDistance { get; private set; }

    //用于驱动动画事件
    [field: SerializeField] 
    public List<SkillAnimationEvent> Events { get; private set; }

}
