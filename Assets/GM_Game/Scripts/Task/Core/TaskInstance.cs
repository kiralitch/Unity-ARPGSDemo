using System.Collections.Generic;
using Game.Task.Config;

/*
 * 进行中的任务实例
 *
 * 存方法：
 * - 每个任务定义同时只存在一个实例（要求 9）
 * - 完全相同目标的并行任务互不影响（各自持有一份 TaskObjectiveRuntime）
 * - 进度跨场景保留在 TaskManager 中
 */

namespace Game.Task.Core
{
    public class TaskInstance
    {
        public string TaskId;                    //任务定义 ID
        public TaskState State = TaskState.InProgress;

        //运行时目标进度（与配置一一对应，顺序一致）
        public List<TaskObjectiveRuntime> Objectives = new List<TaskObjectiveRuntime>();

        public TaskInstance() { }

        public TaskInstance(TaskDefinition def)
        {
            if (def == null) return;

            TaskId = def.TaskId;
            State = TaskState.InProgress;

            Objectives.Clear();
            if (def.Objectives != null)
            {
                foreach (var o in def.Objectives)
                {
                    if (o == null) continue;
                    Objectives.Add(new TaskObjectiveRuntime(o));
                }
            }
        }

        /* 是否全部目标都完成 */
        public bool AreAllObjectivesCompleted()
        {
            if (Objectives.Count == 0) return true;

            foreach (var o in Objectives)
            {
                if (!o.IsCompleted) return false;
            }
            return true;
        }

        /*
         * 与最新配置重绑：
         * - 按相同 targetId 保留进度
         * - 目标列表按新配置增删
         * 返回是否仍在配置中存在（若定义被删除则返回 false）。要求 11 / 12。
         */
        public bool RebindTo(TaskDefinition def)
        {
            if (def == null) return false;
            if (def.TaskId != TaskId) return false;

            // 旧进度按 targetId 建索引
            var oldById = new Dictionary<int, TaskObjectiveRuntime>();
            foreach (var o in Objectives)
            {
                if (!oldById.ContainsKey(o.TargetId)) oldById[o.TargetId] = o;
            }

            var newObjectives = new List<TaskObjectiveRuntime>();
            if (def.Objectives != null)
            {
                foreach (var od in def.Objectives)
                {
                    if (od == null) continue;

                    if (oldById.TryGetValue(od.TargetId, out var old))
                    {
                        // 保留进度，按新配置更新数量与类型
                        old.Rebind(od);
                        newObjectives.Add(old);
                    }
                    else
                    {
                        // 新配置新增目标，进度从零开始
                        newObjectives.Add(new TaskObjectiveRuntime(od));
                    }
                }
            }

            Objectives.Clear();
            Objectives.AddRange(newObjectives);
            return true;
        }

        /* 加进度：给指定目标增加计数。targetIndex < 0 表示按 targetId 匹配。 */
        public bool AddProgress(int targetId, int delta)
        {
            bool changed = false;
            foreach (var o in Objectives)
            {
                if (o.TargetId != targetId) continue;
                if (o.AddProgress(delta)) changed = true;
            }
            return changed;
        }

        /* 加进度：给指定下标目标增加计数（GM 用，越界返回 false） */
        public bool AddProgressByIndex(int index, int delta)
        {
            if (index < 0 || index >= Objectives.Count) return false;
            return Objectives[index].AddProgress(delta);
        }
    }
}
