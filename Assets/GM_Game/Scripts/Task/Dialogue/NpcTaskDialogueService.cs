using System;
using Game.Task.Core;
using UnityEngine;

/*
 * NPC 任务对话服务
 *
 * 项目当前没有对话系统，这里提供最小可用接口 + 示例实现。
 * 流程（要求 4、5、6、7、19）：
 * 1. 玩家与 NPC 对话
 * 2. 若任务池为空：NPC 说日常话，隐藏“接受/拒绝”
 * 3. 若任务池非空：显示“接受 / 拒绝”
 * 4. 接受：从池随机抽 1 个接取（进日志、存档）
 * 5. 拒绝：不抽取、不改变池
 *
 * 接取后不弹窗、不飘字、不音效、不加红点，玩家只能通过任务日志发现（要求 19）。
 *
 * TODO 若后续项目接入正式对话系统，实现 IDialogueSystem 后把 NpcDialoguePanel 换成
 *      对话系统的 UI，本服务逻辑不变。
 */

namespace Game.Task.Dialogue
{
    /// <summary>对话选项数据。</summary>
    public struct DialogueOption
    {
        public string Text;
        public Action OnSelected;

        public DialogueOption(string text, Action onSelected)
        {
            Text = text;
            OnSelected = onSelected;
        }
    }

    /// <summary>NPC 对话数据（供 UI 渲染）。</summary>
    public class DialogueData
    {
        public string SpeakerName;
        public string Content;
        public bool ShowAcceptReject;   //是否显示 接受/拒绝
        public DialogueOption Accept;
        public DialogueOption Decline;

        public static DialogueData IdleTalk(string speaker, string content)
        {
            return new DialogueData
            {
                SpeakerName = speaker,
                Content = content,
                ShowAcceptReject = false,
            };
        }
    }

    /// <summary>对话系统抽象。项目无对话系统时的最小接口。</summary>
    public interface IDialogueSystem
    {
        void ShowDialogue(DialogueData data);
    }

    /// <summary>任务对话服务：由 NPC 调用以发起任务对话。</summary>
    public class NpcTaskDialogueService
    {
        private const string IdleTalkContent = "今天的委托已经派发完了，去忙你自己的事吧。";

        private readonly IDialogueSystem _dialogueSystem;

        public NpcTaskDialogueService(IDialogueSystem dialogueSystem)
        {
            _dialogueSystem = dialogueSystem;
        }

        /// <summary>NPC 发起的任务对话。</summary>
        public void StartDialogue(string npcName = "任务NPC")
        {
            var manager = TaskManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[Task] TaskManager 未初始化，无法发起任务对话。");
                return;
            }

            // 池空：说日常话，隐藏接取选项
            if (manager.IsPoolEmpty())
            {
                _dialogueSystem?.ShowDialogue(DialogueData.IdleTalk(npcName, IdleTalkContent));
                return;
            }

            // 池非空：显示接受/拒绝
            var data = new DialogueData
            {
                SpeakerName = npcName,
                Content = "这里有一个委托，你愿意接吗？",
                ShowAcceptReject = true,
                Accept = new DialogueOption("接受", () => OnAccept(manager)),
                Decline = new DialogueOption("拒绝", () => OnDecline(manager)),
            };

            _dialogueSystem?.ShowDialogue(data);
        }

        private void OnAccept(TaskManager manager)
        {
            var inst = manager.AcceptRandomFromPool();
            if (inst == null)
            {
                Debug.Log("[Task] 接取失败（池空或配置缺失）。");
                return;
            }

            // 接取通知：不弹窗、不飘字、不音效、不加红点（要求 19）。
            // 玩家只能通过任务日志发现新任务。此处仅日志输出，便于开发调试。
            Debug.Log($"[Task] 已接受任务：{inst.TaskId}，请到任务日志查看。");
        }

        private void OnDecline(TaskManager manager)
        {
            manager.Decline();
            Debug.Log("[Task] 已拒绝，任务池未改变。");
        }
    }
}
