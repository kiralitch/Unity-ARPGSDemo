/*
 * 技能时间轴事件处理器接口
 *
 * 每种 SkillEventType 对应一个处理器实现。
 * 处理器只能通过 SkillExecutionContext 拿到施法者，不允许调用状态机切换逻辑。
 */

namespace Game.SkillTimeline
{
    public interface ISkillTimelineEventHandler
    {
        SkillEventType Type { get; }

        /// <summary>事件触发（Time 时刻）</summary>
        void Execute(SkillTimelineEvent evt, SkillExecutionContext context);

        /// <summary>事件持续时间结束（Time + Duration 时刻），主要用于关闭命中盒</summary>
        void Finish(SkillTimelineEvent evt, SkillExecutionContext context);
    }
}
