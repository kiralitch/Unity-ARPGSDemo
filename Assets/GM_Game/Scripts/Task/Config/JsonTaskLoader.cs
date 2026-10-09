using System;
using UnityEngine;
using YooAsset;

/*
 * 任务配置 JSON 加载器（YooAsset）
 *
 * 运行时通过 YooAsset 加载打包进来的 JSON 文本资源，构建 TaskDatabase。
 *
 * TODO 若后续配置量增大，可改为按需加载分表；当前约 10 个任务，全量加载即可。
 */

namespace Game.Task.Config
{
    public static class JsonTaskLoader
    {
        /// <summary>YooAsset 资源地址（完整工程路径）。策划导出 JSON 到该路径。</summary>
        public const string TaskJsonLocation = "Assets/GM_Game/JsonData/TaskJson/TaskTable.json";

        /// <summary>
        /// 通过 YooAsset 异步加载 JSON 并构建数据库。
        /// 若加载失败会回调 null（调用方需自行兜底）。
        /// </summary>
        public static void LoadAsync(Action<TaskDatabase> onLoaded, string location = TaskJsonLocation)
        {
            var handle = YooAssets.LoadAssetAsync<TextAsset>(location);
            handle.Completed += h =>
            {
                if (h.Status == EOperationStatus.Succeed)
                {
                    var text = h.AssetObject as TextAsset;
                    var db = Parse(text != null ? text.text : null);
                    onLoaded?.Invoke(db);
                }
                else
                {
                    Debug.LogError($"[Task] 任务配置加载失败：{location}，Error:{h.LastError}");
                    onLoaded?.Invoke(null);
                }
            };
        }

        /// <summary>同步解析 JSON 文本为数据库（编辑器 / GM 可直接用）。</summary>
        public static TaskDatabase Parse(string json)
        {
            var db = new TaskDatabase();
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[Task] 任务配置 JSON 为空。");
                return db;
            }

            try
            {
                var table = JsonUtility.FromJson<TaskTableJson>(json);
                if (table != null && table.Tasks != null)
                {
                    db.Build(table.Tasks);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Task] 任务配置解析异常：{e.Message}");
            }

            return db;
        }
    }
}
