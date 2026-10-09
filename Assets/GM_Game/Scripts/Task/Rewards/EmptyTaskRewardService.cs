using Game.Task.Config;
using UnityEngine;

/*
 * 空奖励服务：当前任务完成走空奖励流程（要求 17、22）。
 * 奖励内容字段与接口已预留，后续替换本实现即可。
 */

namespace Game.Task.Rewards
{
    public class EmptyTaskRewardService : ITaskRewardService
    {
        public void GrantReward(TaskDefinition def)
        {
            if (def == null) return;

            // 空奖励流程：仅记录日志。TODO 接入实际奖励发放（经验/金币/物品）。
            Debug.Log($"[Task] 任务 {def.TaskId}({def.TaskName}) 完成，发放奖励（当前为空奖励）。");
        }
    }
}
