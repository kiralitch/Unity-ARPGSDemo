using UnityEngine;

/*
 * 任务本地存档服务
 *
 * 遵循项目现有存档模式（PackageLocalData）：JsonUtility + PlayerPrefs。
 * 每个角色独立存档：key 由账号+角色 ID 拼装，互不覆盖（要求存档 1）。
 *
 * TODO 若后续接入项目统一存档系统，可替换本类的底层读写实现，对外接口保持不变。
 */

namespace Game.Task.Save
{
    public static class TaskSaveService
    {
        private const string KeyPrefix = "TaskSaveData_";

        //当前存档归属（登录/选角完成后由外部注入）
        private static string _accountId = "";
        private static string _roleId = "default";

        /// <summary>设置当前存档账号（登录校验通过后调用）。</summary>
        public static void SetAccount(string accountId)
        {
            _accountId = string.IsNullOrEmpty(accountId) ? "" : accountId;
        }

        /// <summary>设置当前角色 ID（同账号多角色，每个角色独立存档）。</summary>
        public static void SetRole(string roleId)
        {
            _roleId = string.IsNullOrEmpty(roleId) ? "default" : roleId;
        }

        public static string CurrentAccount => _accountId;
        public static string CurrentRole => _roleId;

        private static string BuildKey()
        {
            return $"{KeyPrefix}{_accountId}_{_roleId}";
        }

        /// <summary>读取当前角色的任务存档；不存在返回 null。</summary>
        public static TaskSaveData Load()
        {
            string key = BuildKey();
            if (!PlayerPrefs.HasKey(key)) return null;

            try
            {
                string json = PlayerPrefs.GetString(key);
                return JsonUtility.FromJson<TaskSaveData>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Task] 任务存档解析失败：{e.Message}");
                return null;
            }
        }

        /// <summary>写入当前角色的任务存档。</summary>
        public static void Save(TaskSaveData data)
        {
            if (data == null) return;

            string key = BuildKey();
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }

        /// <summary>删除当前角色的任务存档（GM 清空用）。</summary>
        public static void Delete()
        {
            PlayerPrefs.DeleteKey(BuildKey());
            PlayerPrefs.Save();
        }
    }
}
