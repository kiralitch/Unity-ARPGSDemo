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

    //时间轴事件（技能编辑器写入）。为 null 时表示该技能没有配置时间轴，运行时直接跳过
    //注意：必须是普通字段而不是自动属性，否则 Unity 会把数据存在 <Timeline>k__BackingField 上，
    //编辑器里的 FindPropertyRelative("Timeline") 找不到，时间轴编辑器无法写入
    [SerializeField]
    [Tooltip("由技能时间轴编辑器维护：每段连招动画上的音效/特效/伤害事件")]
    private Game.SkillTimeline.SkillTimeline timeline;

    public Game.SkillTimeline.SkillTimeline Timeline => timeline;

}
