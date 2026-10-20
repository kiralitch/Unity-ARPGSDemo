using UnityEngine;

/*
 * 技能时间轴触发的全局钩子
 *
 * 战斗逻辑不直接依赖「伤害是否由时间轴产生」，但武器命中盒只知道「开/关」，
 * 时间轴需要一个不经状态机、又能被外部观察到的最小通道：
 *   - 编辑器预览通过 OnPreviewDamageTick 画判定球，不产生真实伤害
 *   - 以后的统计/提示可订阅 OnTimelineDamage
 * 默认没有任何订阅者，行为与现状一致。
 */

namespace Game.SkillTimeline
{
    public static class SkillTimelineRuntimeHooks
    {
        /// <summary>时间轴伤害结算开始：caster、伤害值、持续时间</summary>
        public static event System.Action<CommonActor, float, float> OnTimelineDamage;

        /// <summary>预览模式下的伤害判定（只用于画 Gizmo）</summary>
        public static event System.Action<Transform, float> OnPreviewDamageTick;

        public static void RaiseTimelineDamage(CommonActor caster, float damage, float duration)
        {
            OnTimelineDamage?.Invoke(caster, damage, duration);
        }

        public static void RaisePreviewDamageTick(Transform caster, float duration)
        {
            OnPreviewDamageTick?.Invoke(caster, duration);
        }
    }
}
