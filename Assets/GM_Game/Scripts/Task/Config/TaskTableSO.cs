using System.Collections.Generic;

/*
 * 任务配置表（SO 编辑器源数据）
 *
 * 用法：在编辑器里维护任务列表 -> 通过 TaskJsonExporter 导出 JSON。
 * 运行时由 JsonTaskLoader 加载 JSON 构建 TaskDatabase，不直接读取本 SO。
 */

namespace Game.Task.Config
{
    using UnityEngine;

    [CreateAssetMenu(menuName = "Task/TaskTable", fileName = "TaskTable")]
    public class TaskTableSO : ScriptableObject
    {
        [field: SerializeField]
        public List<TaskDefinition> Tasks { get; private set; } = new List<TaskDefinition>();

        /* 读取时兜底，避免空引用 */
        public List<TaskDefinition> GetTasks()
        {
            if (Tasks == null) Tasks = new List<TaskDefinition>();
            return Tasks;
        }
    }
}
