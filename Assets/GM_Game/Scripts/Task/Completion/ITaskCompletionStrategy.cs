using Game.Task.Config;
using Game.Task.Core;

/*
 * 任务完成策略接口
 *
 * 当前实现：AutoCompleteStrategy（目标全完成即自动完成）
 * 预留实现：NpcSubmitStrategy（回到 NPC 提交），后续主线扩展时新增即可。
 *
 * 该接口只负责“判断是否满足完成条件”，
 * 真正的完成流程（奖励、弹窗、移除、回池、存档）由 TaskManager 统一执行。
 */

namespace Game.Task.Completion
{
    public interface ITaskCompletionStrategy
    {
        /// <summary>该策略是否适用此任务定义。</summary>
        bool CanHandle(TaskDefinition def);

        /// <summary>当前是否满足完成条件。</summary>
        bool IsCompleted(TaskDefinition def, TaskInstance instance);
    }
}
