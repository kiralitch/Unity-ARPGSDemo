using System.Collections.Generic;

/*
 * 任务池
 *
 * - 全局一个逻辑池，但每个存档独立一份（由 TaskManager 持有实例）
 * - 池内容 = 所有“可进入日常池”的任务定义 ID
 * - 接取时随机抽 1 个并从池移除；完成后回池
 */

namespace Game.Task.Core
{
    public class TaskPool
    {
        private readonly List<string> _ids = new List<string>();

        public int Count => _ids.Count;
        public bool IsEmpty => _ids.Count == 0;

        /* 只读访问（GM / 调试打印用） */
        public IReadOnlyList<string> Ids => _ids;

        public bool Contains(string taskId)
        {
            return !string.IsNullOrEmpty(taskId) && _ids.Contains(taskId);
        }

        /* 按一组 ID 重置池（初始化 / 清空重新初始化用） */
        public void Reset(IEnumerable<string> taskIds)
        {
            _ids.Clear();
            if (taskIds == null) return;
            foreach (var id in taskIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                if (_ids.Contains(id)) continue;
                _ids.Add(id);
            }
        }

        /* 随机抽出 1 个任务 ID，并从池中移除 */
        public string DrawRandom()
        {
            if (_ids.Count == 0) return null;

            int index = UnityEngine.Random.Range(0, _ids.Count);
            string id = _ids[index];
            _ids.RemoveAt(index);
            return id;
        }

        /* 归还任务到池（完成后回池）。已存在则不重复添加。 */
        public void Return(string taskId)
        {
            if (string.IsNullOrEmpty(taskId)) return;
            if (_ids.Contains(taskId)) return;
            _ids.Add(taskId);
        }

        public void Clear()
        {
            _ids.Clear();
        }

        /* 导出为可存档的 ID 列表 */
        public List<string> ToList()
        {
            return new List<string>(_ids);
        }
    }
}
