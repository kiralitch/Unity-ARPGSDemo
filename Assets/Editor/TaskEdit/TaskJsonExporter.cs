#if UNITY_EDITOR
using System.IO;
using Game.Task.Config;
using UnityEditor;
using UnityEngine;

/*
 * 任务配置 JSON 导出
 *
 * 从 TaskTableSO 导出为 JSON，供运行时（YooAsset）加载。
 * 运行时以 JSON 为任务配置资源，热更后下次登录生效。
 */

namespace Game.Task.EditorTools
{
    public static class TaskJsonExporter
    {
        /// <summary>默认导出路径（与 JsonTaskLoader.TaskJsonLocation 保持一致）。</summary>
        public const string DefaultOutputPath = "Assets/GM_Game/JsonData/TaskJson/TaskTable.json";

        [MenuItem("Tools/Task/导出 JSON")]
        public static void ExportFromMenu()
        {
            var so = SelectTaskTable();
            if (so == null) return;
            Export(so, DefaultOutputPath);
        }

        /// <summary>导出指定 SO 到指定路径。</summary>
        public static void Export(TaskTableSO so, string outputPath)
        {
            if (so == null)
            {
                Debug.LogError("[Task] 导出失败：TaskTableSO 为空。");
                return;
            }

            var table = new TaskTableJson { Version = 1, Tasks = so.GetTasks() };
            string json = JsonUtility.ToJson(table, true);

            string dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(outputPath, json);
            AssetDatabase.Refresh();

            Debug.Log($"[Task] 已导出任务配置 JSON：{outputPath}（{table.Tasks.Count} 个任务）");
        }

        /// <summary>唯一 ID 重复警告（不阻塞，默认策划保证唯一）。</summary>
        public static void WarnDuplicateIds(TaskTableSO so)
        {
            if (so == null) return;

            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var t in so.GetTasks())
            {
                if (t == null) continue;
                if (string.IsNullOrEmpty(t.TaskId))
                {
                    Debug.LogWarning("[Task] 存在空 TaskId 的任务定义。");
                    continue;
                }
                if (!seen.Add(t.TaskId))
                {
                    Debug.LogWarning($"[Task] 任务 ID 重复：{t.TaskId}（请策划保证唯一）");
                }
            }
        }

        private static TaskTableSO SelectTaskTable()
        {
            string path = EditorUtility.OpenFilePanelWithFilters(
                "选择 TaskTableSO 资产", "Assets", new[] { "TaskTableSO", "asset" });

            if (string.IsNullOrEmpty(path)) return null;

            // 转成项目相对路径
            if (path.StartsWith(Application.dataPath))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
            }

            return AssetDatabase.LoadAssetAtPath<TaskTableSO>(path);
        }
    }
}
#endif
