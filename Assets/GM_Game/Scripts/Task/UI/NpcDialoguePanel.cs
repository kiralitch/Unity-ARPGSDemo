using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Task.Dialogue;

/*
 * NPC 对话面板（最小可用示例实现）
 *
 * 实现 IDialogueSystem，供 NpcTaskDialogueService 驱动。
 * - 显示 NPC 名称与对话内容
 * - 池非空时显示“接受 / 拒绝”按钮
 * - 池空时隐藏按钮，仅显示日常话
 *
 * 项目暂无对话系统，故提供该示例。若已有对话系统，请改用其 UI 并通过 NpcTaskDialogueService 接入。
 */

namespace Game.Task.UI
{
    public class NpcDialoguePanel : MonoBehaviour, IDialogueSystem
    {
        [SerializeField, Header("面板根")] private GameObject root;
        [SerializeField, Header("NPC 名称")] private TMP_Text speakerText;
        [SerializeField, Header("对话内容")] private TMP_Text contentText;
        [SerializeField, Header("接受按钮")] private Button acceptButton;
        [SerializeField, Header("拒绝按钮")] private Button declineButton;
        [SerializeField, Header("接受按钮文本")] private TMP_Text acceptText;
        [SerializeField, Header("拒绝按钮文本")] private TMP_Text declineText;

        private DialogueData _current;

        private void Awake()
        {
            if (root != null) root.SetActive(false);
        }

        /// <summary>由 TaskBootstrap 注入任务对话服务（当前由服务主动 ShowDialogue，无需持有）。</summary>
        public void BindService(NpcTaskDialogueService service)
        {
            // 服务通过 ShowDialogue 回调驱动 UI，无需额外绑定
        }

        public void ShowDialogue(DialogueData data)
        {
            if (data == null) return;
            _current = data;

            if (root != null) root.SetActive(true);
            if (speakerText != null) speakerText.text = data.SpeakerName;
            if (contentText != null) contentText.text = data.Content;

            bool show = data.ShowAcceptReject;

            SetupButton(acceptButton, acceptText, data.Accept, show, OnAcceptClicked);
            SetupButton(declineButton, declineText, data.Decline, show, OnDeclineClicked);
        }

        private void SetupButton(Button button, TMP_Text label, DialogueOption option, bool show, UnityEngine.Events.UnityAction clickHandler)
        {
            if (button == null) return;

            button.gameObject.SetActive(show);
            if (!show) return;

            if (label != null) label.text = option.Text;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(clickHandler);
        }

        private void OnAcceptClicked()
        {
            var option = _current.Accept;
            Close();
            option.OnSelected?.Invoke();
        }

        private void OnDeclineClicked()
        {
            var option = _current.Decline;
            Close();
            option.OnSelected?.Invoke();
        }

        public void Close()
        {
            if (root != null) root.SetActive(false);
            _current = null;
        }
    }
}
