#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Game.Task.Config;
using Game.Task.Core;
using Game.Task.Dialogue;
using UnityEngine;

/*
 * 任务系统 GM 调试命令
 *
 * 发布版移除：整个文件的实现体用 #if UNITY_EDITOR || DEVELOPMENT_BUILD 包裹，
 * 发布版（非 Development Build）不会编译进包体。
 *
 * 命令：接取、强制完成、清空、打印状态、添加进度。
 * 触发方式：运行时按 F 键打开简易按钮面板（GameObject 挂 TaskGMConsole 组件）。
 */

namespace Game.Task.GM
{
    public class TaskGMConsole : MonoBehaviour
    {
        [SerializeField] private bool showConsole = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.P;

        // 添加进度用的输入
        private string _taskIdInput = "";
        private int _objectiveIndex = 0;
        private int _delta = 1;

        private NpcTaskDialogueService _dialogueService;

        private void Start()
        {
            // 复用 NPC 对话服务完成“接取”命令（随机抽 1 个）
            _dialogueService = new NpcTaskDialogueService(null);
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey)) showConsole = !showConsole;
        }

        private void OnGUI()
        {
            if (!showConsole) return;
            if (TaskManager.Instance == null)
            {
                GUILayout.Label("[Task GM] TaskManager 未初始化");
                return;
            }

            var mgr = TaskManager.Instance;

            GUILayout.BeginArea(new Rect(10, 10, 360, 360), GUI.skin.box);
            GUILayout.Label($"== 任务 GM (F 隐藏) ==  池:{mgr.Pool.Count} 进行中:{mgr.InProgressTasks.Count}");

            if (GUILayout.Button("1. 接取（随机抽 1 个）"))
            {
                var inst = mgr.AcceptRandomFromPool();
                if (inst == null) Debug.LogWarning("[Task GM] 接取失败：池为空");
                else Debug.Log($"[Task GM] 已接取：{inst.TaskId}");
            }

            GUILayout.Space(4);
            GUILayout.Label("任务 ID：");
            _taskIdInput = GUILayout.TextField(_taskIdInput);

            if (GUILayout.Button("2. 强制完成（输入的任务 ID）"))
            {
                mgr.ForceComplete(_taskIdInput);
            }

            if (GUILayout.Button("3. 清空（清空全部并重新初始化池）"))
            {
                mgr.ClearAll();
            }

            if (GUILayout.Button("4. 打印状态"))
            {
                mgr.DumpState();
            }

            GUILayout.Space(4);
            GUILayout.Label("添加进度：目标下标（-1=全部）");
            string idxStr = GUILayout.TextField(_objectiveIndex.ToString());
            int.TryParse(idxStr, out _objectiveIndex);
            string deltaStr = GUILayout.TextField(_delta.ToString());
            int.TryParse(deltaStr, out _delta);

            if (GUILayout.Button("5. 添加进度（输入的任务 ID）"))
            {
                bool ok = mgr.AddProgress(_taskIdInput, _objectiveIndex, _delta);
                Debug.Log($"[Task GM] 添加进度 {(ok ? "成功" : "失败")}：task={_taskIdInput} idx={_objectiveIndex} delta={_delta}");
            }

            GUILayout.EndArea();
        }
    }
}
#endif
