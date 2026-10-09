using Game.Task.Config;
using Game.Task.Core;

/*
 * 自动完成策略：所有目标达成即完成。
 * 当前任务唯一的完成策略。
 */

namespace Game.Task.Completion
{
    public class AutoCompleteStrategy : ITaskCompletionStrategy
    {
        public bool CanHandle(TaskDefinition def)
        {
            return def != null && def.FinishType == TaskFinishType.Auto;
        }

        public bool IsCompleted(TaskDefinition def, TaskInstance instance)
        {
            if (instance == null) return false;
            return instance.AreAllObjectivesCompleted();
        }
    }
}
