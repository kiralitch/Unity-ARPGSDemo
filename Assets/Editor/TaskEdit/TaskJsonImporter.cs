#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Game.Task.Config;
using UnityEditor;
using UnityEngine;

/*
 * 任务配置 JSON 反向导入
 *
 * 从 JSON 读取任务定义，创建或更新 TaskTableSO 里的任务。
 * 匹配规则：按 TaskId 匹配；已存在则更新，不存在则新增。
 */

namespace Game.Task.EditorTools
{
    public static class TaskJsonImporter
    {
        [MenuItem("Tools/Task/从 JSON 导入")]
        public static void ImportFromMenu()
        {
            var so = SelectTaskTable();
            if (so == null) return;

            string path = EditorUtility.OpenFilePanelWithFilters(
                "选择任务配置 JSON", "Assets", new[] { "JSON", "json" });

            if (string.IsNullOrEmpty(path)) return;
            if (!File.Exists(path))
            {
                Debug.LogError($"[Task] JSON 不存在：{path}");
                return;
            }

            Import(so, path);
        }

        /// <summary>将 JSON 导入到指定 SO。</summary>
        public static void Import(TaskTableSO so, string jsonPath)
        {
            if (so == null)
            {
                Debug.LogError("[Task] 导入失败：TaskTableSO 为空。");
                return;
            }

            string json = File.ReadAllText(jsonPath);
            var table = JsonUtility.FromJson<TaskTableJson>(json);
            if (table == null || table.Tasks == null)
            {
                Debug.LogError("[Task] 导入失败：JSON 解析为空。");
                return;
            }

            var list = so.GetTasks();

            // 现有任务按 TaskId 建索引
            var existing = new Dictionary<string, TaskDefinition>();
            foreach (var t in list)
            {
                if (t != null && !string.IsNullOrEmpty(t.TaskId) && !existing.ContainsKey(t.TaskId))
                {
                    existing.Add(t.TaskId, t);
                }
            }

            int added = 0, updated = 0;
            foreach (var def in table.Tasks)
            {
                if (def == null || string.IsNullOrEmpty(def.TaskId)) continue;

                if (existing.TryGetValue(def.TaskId, out var old))
                {
                    CopyInto(old, def); // 更新
                    updated++;
                }
                else
                {
                    list.Add(def); // 新增
                    added++;
                }
            }

            EditorUtility.SetDirty(so);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            TaskJsonExporter.WarnDuplicateIds(so);

            Debug.Log($"[Task] JSON 导入完成：新增 {added}，更新 {updated}，共 {list.Count} 个任务。");
        }

        /* 把源数据字段复制到目标对象（保持资产引用不被替换） */
        private static void CopyInto(TaskDefinition dst, TaskDefinition src)
        {
            dst.SerialId = src.SerialId;
            dst.TaskName = src.TaskName;
            dst.Description = src.Description;
            dst.AcceptType = src.AcceptType;
            dst.bEnterDailyPool = src.bEnterDailyPool;
            dst.FinishType = src.FinishType;
            dst.Objectives = src.Objectives;
            dst.Reward = src.Reward;
            dst.bRepeatable = src.bRepeatable;
            dst.PreTaskIds = src.PreTaskIds;
            dst.NextTaskId = src.NextTaskId;
            dst.RequiredLevel = src.RequiredLevel;
        }

        private static TaskTableSO SelectTaskTable()
        {
            string path = EditorUtility.OpenFilePanelWithFilters(
                "选择 TaskTableSO 资产", "Assets", new[] { "TaskTableSO", "asset" });

            if (string.IsNullOrEmpty(path)) return null;

            if (path.StartsWith(Application.dataPath))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
            }

            return AssetDatabase.LoadAssetAtPath<TaskTableSO>(path);
        }
    }
}
#endif
