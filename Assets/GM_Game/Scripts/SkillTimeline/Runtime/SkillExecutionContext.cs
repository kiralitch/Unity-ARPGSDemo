using UnityEngine;

/*
 * 技能时间轴事件的执行上下文
 *
 * 只暴露处理器需要的只读信息，处理器不直接操作状态机/动画图，
 * 从而保证「技能编辑器只做接缝，不改战斗逻辑」。
 */

namespace Game.SkillTimeline
{
    public sealed class SkillExecutionContext
    {
        /// <summary>施法者（Player 或 Enemy）</summary>
        public CommonActor Caster { get; }

        /// <summary>施法者身上的动画事件触发器（用于解析挂点骨骼）</summary>
        public Transform CasterRoot { get; }

        /// <summary>当前连招段索引</summary>
        public int ClipIndex { get; }

        /// <summary>该段动画的基础伤害（已乘玩家/敌人倍率之前的值）</summary>
        public float BaseDamage { get; }

        /// <summary>是否由编辑器预览驱动（预览时不产生任何玩法副作用）</summary>
        public bool IsPreview { get; }

        public SkillExecutionContext(CommonActor caster, Transform casterRoot, int clipIndex, float baseDamage, bool isPreview)
        {
            Caster = caster;
            CasterRoot = casterRoot;
            ClipIndex = clipIndex;
            BaseDamage = baseDamage;
            IsPreview = isPreview;
        }
    }
}
