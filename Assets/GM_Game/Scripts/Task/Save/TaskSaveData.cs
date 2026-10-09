using System;
using System.Collections.Generic;

/*
 * 任务存档数据
 *
 * 保存内容（要求存档）：
 * - 任务池剩余任务 ID 列表
 * - 进行中任务的 taskId
 * - 每个目标的当前计数（按 targetId 记录）
 *
 * 不保存：已完成历史、时间戳、随机种子、选择、轮次历史。
 */

namespace Game.Task.Save
{
    [Serializable]
    public class TaskSaveData
    {
        /// <summary>任务池中剩余的任务 ID 列表（接取后移除，完成后回池）。</summary>
        public List<string> PoolTaskIds = new List<string>();

        /// <summary>进行中的任务实例。</summary>
        public List<TaskInstanceSave> InProgressTasks = new List<TaskInstanceSave>();
    }

    [Serializable]
    public class TaskInstanceSave
    {
        public string TaskId;

        /// <summary>各目标的进度。按 targetId 匹配，配置变化时保留进度。</summary>
        public List<TaskObjectiveSave> Objectives = new List<TaskObjectiveSave>();
    }

    [Serializable]
    public class TaskObjectiveSave
    {
        public int TargetId;
        public int CurrentCount;
    }
}
