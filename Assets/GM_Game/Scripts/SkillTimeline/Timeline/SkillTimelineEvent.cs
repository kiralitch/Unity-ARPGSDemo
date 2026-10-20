using System;
using UnityEngine;

/*
 * 技能时间轴上的单个事件
 *
 * 与现有 SkillAnimationEvent 的区别：
 *   - SkillAnimationEvent 只有 eventName + time，且运行时从未被消费
 *   - 这里按类型拆分字段，运行时由 SkillTimelineExecutor 按时间点执行
 *
 * 只作为 AbilitiesData 的可选扩展字段存在，不替换、不修改原有 Events 列表。
 */

namespace Game.SkillTimeline
{
    [Serializable]
    public class SkillTimelineEvent
    {
        [Tooltip("事件类型")]
        public SkillEventType Type = SkillEventType.PlaySound;

        [Tooltip("触发时刻（秒），相对于战斗动画开始")]
        public float Time;

        [Tooltip("持续时间（秒）。仅 Damage 使用：命中盒从 Time 开启，持续到 Time + Duration")]
        public float Duration;

        [Tooltip("资源地址（YooAsset）。PlaySound / SpawnVfx 使用")]
        public string AssetPath;

        [Tooltip("挂点骨骼名（SpawnVfx 使用）。为空则生成在世界坐标")]
        public string AttachPoint;

        [Tooltip("音量（PlaySound 使用）")]
        [Range(0f, 1f)]
        public float Volume = 1f;

        [Tooltip("伤害倍率（Damage 使用）。最终伤害 = 技能伤害 × 该倍率")]
        public float DamageScale = 1f;

        [Tooltip("运行时标记：本段动画内已触发，防止重复触发。不参与序列化保存以外的用途")]
        [NonSerialized]
        public bool IsTriggered;

        public SkillTimelineEvent()
        {
        }

        public SkillTimelineEvent(SkillEventType type, float time)
        {
            Type = type;
            Time = time;
        }

        public float EndTime => Time + Mathf.Max(0f, Duration);

        /// <summary>深拷贝，避免编辑器预览与运行时共用同一个实例</summary>
        public SkillTimelineEvent Clone()
        {
            return new SkillTimelineEvent
            {
                Type = Type,
                Time = Time,
                Duration = Duration,
                AssetPath = AssetPath,
                AttachPoint = AttachPoint,
                Volume = Volume,
                DamageScale = DamageScale,
                IsTriggered = false,
            };
        }

        /// <summary>编辑器时间轴显示文本</summary>
        public string GetTimelineLabel()
        {
            switch (Type)
            {
                case SkillEventType.PlaySound:
                    return string.IsNullOrEmpty(AssetPath) ? "音效(未配置)" : System.IO.Path.GetFileNameWithoutExtension(AssetPath);
                case SkillEventType.SpawnVfx:
                    return string.IsNullOrEmpty(AssetPath) ? "特效(未配置)" : System.IO.Path.GetFileNameWithoutExtension(AssetPath);
                case SkillEventType.Damage:
                    return Duration > 0f ? $"伤害 x{DamageScale:0.##} ({Duration:0.##}s)" : $"伤害 x{DamageScale:0.##}";
                default:
                    return Type.GetDisplayName();
            }
        }
    }
}
