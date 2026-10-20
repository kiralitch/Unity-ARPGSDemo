using UnityEngine;

/*
 * 伤害结算处理器
 *
 * 目标：「在时间轴的某个时刻进行伤害结算」，但不重写伤害公式、不改命中判定。
 * 做法：复用现有 SharedWeaponHitBox：
 *   - Execute：把「技能伤害 × DamageScale」写入命中盒缓存伤害，并开启命中盒
 *   - Finish ：关闭命中盒
 *
 * 这样命中判定仍然是 SharedWeaponHitBox.OnTriggerEnter -> IDamageable.TakeDamage，
 * 伤害倍率仍然是 Player.CommonDamageScales（由 SkillTimelineTicker 预先乘好），
 * 时间轴只决定「何时开、何时关」，与现有玩法完全一致。
 *
 * 注意：若命中盒已被动画关键帧事件打开（旧的 CommonAnimationEventTrigger 流程），
 * 时间轴会在此基础上再设置一次伤害并保持开启，不会关闭别人的开启状态。
 */

namespace Game.SkillTimeline
{
    public sealed class DamageHandler : ISkillTimelineEventHandler
    {
        public SkillEventType Type => SkillEventType.Damage;

        public void Execute(SkillTimelineEvent evt, SkillExecutionContext context)
        {
            if (context == null || evt == null) return;

            float damage = Mathf.Max(0f, context.BaseDamage) * Mathf.Max(0f, evt.DamageScale);

            if (context.IsPreview)
            {
                SkillTimelineRuntimeHooks.RaisePreviewDamageTick(context.CasterRoot, evt.Duration);
                return;
            }

            var hitBox = FindHitBox(context.CasterRoot);
            if (hitBox == null)
            {
                Debug.LogWarning($"[SkillTimeline] {context.CasterRoot?.name} 上找不到 SharedWeaponHitBox，伤害事件被跳过（仍会广播 OnTimelineDamage）。");
            }
            else
            {
                hitBox.SetCachedDamage(damage);
                hitBox.EnableHitBox();
            }

            SkillTimelineRuntimeHooks.RaiseTimelineDamage(context.Caster, damage, evt.Duration);
        }

        public void Finish(SkillTimelineEvent evt, SkillExecutionContext context)
        {
            if (context == null || context.IsPreview) return;

            FindHitBox(context.CasterRoot)?.DisableHitBox();
        }

        /// <summary>从施法者根节点向下找武器命中盒（Player 与 Enemy 都挂在子节点上）</summary>
        private static SharedWeaponHitBox FindHitBox(Transform root)
        {
            if (root == null) return null;

            if (root.TryGetComponent(out SharedWeaponHitBox direct)) return direct;

            var hits = root.GetComponentsInChildren<SharedWeaponHitBox>(true);
            return hits != null && hits.Length > 0 ? hits[0] : null;
        }
    }
}
