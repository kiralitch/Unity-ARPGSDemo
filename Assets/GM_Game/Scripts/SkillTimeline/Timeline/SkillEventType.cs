using System;

/*
 * 技能时间轴事件类型
 *
 * 只放「编辑器可填、运行时可执行」的事件种类。
 * 新增事件类型时：
 *   1. 在这里加枚举值
 *   2. 在 SkillTimelineExecutor 里登记对应的 ISkillTimelineEventHandler
 *   3. 在编辑器 SkillTimelineWindow 的轨道定义里加一条轨道
 * 战斗状态机（Player_AttackState / Enemy_AttackState）不需要任何改动。
 */

namespace Game.SkillTimeline
{
    public enum SkillEventType
    {
        /// <summary>播放音效：AssetPath 为 YooAsset 地址</summary>
        PlaySound = 0,

        /// <summary>生成特效：AssetPath 为 YooAsset 地址，AttachPoint 为空则生成在世界坐标</summary>
        SpawnVfx = 1,

        /// <summary>伤害结算：在时间轴 t 时刻开启命中盒，持续 Duration 秒</summary>
        Damage = 2,

        /// <summary>预留：镜头震屏</summary>
        CameraShake = 3,

        /// <summary>预留：移动位移</summary>
        Motion = 4,
    }

    public static class SkillEventTypeUtil
    {
        /// <summary>编辑器轨道显示名</summary>
        public static string GetDisplayName(this SkillEventType type)
        {
            switch (type)
            {
                case SkillEventType.PlaySound: return "音效";
                case SkillEventType.SpawnVfx: return "特效";
                case SkillEventType.Damage: return "伤害";
                case SkillEventType.CameraShake: return "震屏";
                case SkillEventType.Motion: return "位移";
                default: return type.ToString();
            }
        }

        /// <summary>该类型当前是否已有运行时实现（没有则只保存数据，不执行）</summary>
        public static bool IsImplemented(this SkillEventType type)
        {
            return type == SkillEventType.PlaySound
                   || type == SkillEventType.SpawnVfx
                   || type == SkillEventType.Damage;
        }
    }
}
