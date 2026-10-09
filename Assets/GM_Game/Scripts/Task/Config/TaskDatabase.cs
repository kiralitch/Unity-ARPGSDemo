using System.Collections.Generic;

/*
 * 任务数据库：运行时的任务定义索引
 *
 * 由 JsonTaskLoader 从 JSON 构建。
 * - 提供按 ID / 按序列号查询
 * - 汇总“可进入日常池”的任务，用于初始化任务池
 */

namespace Game.Task.Config
{
    public class TaskDatabase
    {
        private readonly Dictionary<string, TaskDefinition> _byId = new Dictionary<string, TaskDefinition>();
        private readonly List<TaskDefinition> _all = new List<TaskDefinition>();

        public IReadOnlyList<TaskDefinition> All => _all;
        public int Count => _all.Count;
        public bool IsEmpty => _all.Count == 0;

        /* 用一组任务定义构建数据库 */
        public void Build(IEnumerable<TaskDefinition> defs)
        {
            _byId.Clear();
            _all.Clear();

            if (defs == null) return;

            foreach (var def in defs)
            {
                if (def == null || string.IsNullOrEmpty(def.TaskId)) continue;

                //重复 ID 不阻塞：保留第一条，后续重复跳过（策划保证唯一）
                if (_byId.ContainsKey(def.TaskId)) continue;

                _byId.Add(def.TaskId, def);
                _all.Add(def);
            }

            _all.Sort((a, b) => a.SerialId.CompareTo(b.SerialId));
        }

        public TaskDefinition Get(string taskId)
        {
            if (string.IsNullOrEmpty(taskId)) return null;
            _byId.TryGetValue(taskId, out var def);
            return def;
        }

        public bool Contains(string taskId)
        {
            return !string.IsNullOrEmpty(taskId) && _byId.ContainsKey(taskId);
        }

        /* 汇总所有“可进入日常池”的任务 ID（用于初始化任务池） */
        public List<string> GetDailyPoolTaskIds()
        {
            var result = new List<string>();
            foreach (var def in _all)
            {
                if (def.bEnterDailyPool) result.Add(def.TaskId);
            }
            return result;
        }
    }
}
