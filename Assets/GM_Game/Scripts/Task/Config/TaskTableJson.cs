using System;
using System.Collections.Generic;

/*
 * 任务配置表 JSON 根对象
 *
 * 注意：JsonUtility 不支持顶层数组，因此用一个包装对象承载任务列表。
 * 导出格式示例：
 * {
 *   "Version": 1,
 *   "Tasks": [ { "TaskId": "daily_kill_slime", ... } ]
 * }
 */

namespace Game.Task.Config
{
    [Serializable]
    public class TaskTableJson
    {
        public int Version = 1;
        public List<TaskDefinition> Tasks = new List<TaskDefinition>();
    }
}
