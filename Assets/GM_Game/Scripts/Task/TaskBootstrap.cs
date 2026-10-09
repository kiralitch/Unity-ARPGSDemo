using UnityEngine;
using Game.Task.Config;
using Game.Task.Core;
using Game.Task.Dialogue;
using Game.Task.Save;
using Game.Task.UI;

/*
 * 任务系统启动器
 *
 * - 创建 TaskManager（DontDestroyOnLoad 跨场景持久化）
 * - 设置存档账号/角色
 * - 初始化（加载 JSON 配置 + 读档）
 * - 绑定任务日志与完成弹窗 UI
 *
 * 放置方式：在 Boot / 登录场景放置一个空物体挂载本组件即可。
 * 若项目已有服务容器/启动流程，也可直接调用 TaskManager.Instance.Init()。
 */

namespace Game.Task
{
    public class TaskBootstrap : MonoBehaviour
    {
        [Header("存档归属（同账号多角色，各自独立存档）")]
        [SerializeField] private string accountId = "";
        [SerializeField] private string roleId = "default";

        [Header("UI")]
        [SerializeField] private TaskLogView taskLogView;
        [SerializeField] private CompletionPopupView completionPopupView;

        [Header("NPC 对话（示例对话面板，可选）")]
        [SerializeField] private NpcDialoguePanel dialoguePanel;

        private TaskLogController _logController;
        private CompletionPopupController _popupController;
        private NpcTaskDialogueService _dialogueService;

        private void Awake()
        {
            EnsureManager();

            // 设置存档归属
            TaskSaveService.SetAccount(accountId);
            TaskSaveService.SetRole(roleId);
        }

        private void Start()
        {
            // 初始化任务系统
            TaskManager.Instance.Init(() =>
            {
                SetupUI();
            });
        }

        private void EnsureManager()
        {
            if (TaskManager.Instance != null) return;

            var go = new GameObject("TaskManager");
            go.AddComponent<TaskManager>(); // TaskManager.Awake 内部 DontDestroyOnLoad
        }

        private void SetupUI()
        {
            // 任务日志
            if (taskLogView != null)
            {
                _logController = new TaskLogController(taskLogView);
                _logController.Bind();
            }

            // 完成弹窗
            if (completionPopupView != null)
            {
                _popupController = new CompletionPopupController(completionPopupView);
                _popupController.Bind();
            }

            // NPC 对话
            if (dialoguePanel != null)
            {
                _dialogueService = new NpcTaskDialogueService(dialoguePanel);
                dialoguePanel.BindService(_dialogueService);
            }
        }

        private void OnDestroy()
        {
            _logController?.Unbind();
            _popupController?.Unbind();
        }

        /// <summary>供外部（如 NPC 交互）获取任务对话服务。</summary>
        public NpcTaskDialogueService DialogueService => _dialogueService;
    }
}
