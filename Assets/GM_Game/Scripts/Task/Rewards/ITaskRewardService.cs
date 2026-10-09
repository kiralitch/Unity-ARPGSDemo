using Game.Task.Config;

/*
 * 任务奖励服务接口
 *
 * 当前实现：EmptyTaskRewardService（空奖励，仅打印日志）。
 * 后续可新增正式奖励实现（发经验/金币/物品）。
 */

namespace Game.Task.Rewards
{
    public interface ITaskRewardService
    {
        /// <summary>发放奖励。当前为空实现。</summary>
        void GrantReward(TaskDefinition def);
    }
}
