using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
 * 任务完成弹窗视图
 *
 * 要求（18、验收 7）：
 * - 只显示“【名称】任务完成”
 * - 无交互（不响应点击）
 * - 阻塞输入（打开期间暂停玩家输入）
 * - 自动关闭
 * - 多个完成排队逐个弹
 *
 * 若项目已有完成弹窗可替换本实现；此处为最小可用阻塞弹窗。
 */

namespace Game.Task.UI
{
    public class CompletionPopupView : UIBase
    {
        [SerializeField, Header("弹窗根")] private GameObject root;
        [SerializeField, Header("文本")] private TMP_Text messageText;
        [SerializeField, Header("显示时长（秒）")] private float showDuration = 1.6f;

        /// <summary>全屏输入拦截层（可选，用于阻塞点击）。</summary>
        [SerializeField, Header("输入拦截层")] private GameObject inputBlocker;

        private readonly Queue<string> _queue = new Queue<string>();
        private Coroutine _running;

        /// <summary>当弹窗显隐时回调（用于暂停/恢复玩家输入）。true=弹窗显示中。</summary>
        public System.Action<bool> OnBlockingChanged;

        private void Awake()
        {
            HideImmediate();
        }

        /// <summary>入队一条完成提示。</summary>
        public void Enqueue(string taskName)
        {
            _queue.Enqueue(taskName);
            if (_running == null) _running = StartCoroutine(ProcessQueue());
        }

        private IEnumerator ProcessQueue()
        {
            while (_queue.Count > 0)
            {
                string name = _queue.Dequeue();
                Show(name);
                yield return new WaitForSeconds(showDuration);
                HideImmediate();
            }

            _running = null;
        }

        private void Show(string taskName)
        {
            if (root != null) root.SetActive(true);
            if (inputBlocker != null) inputBlocker.SetActive(true);
            if (messageText != null) messageText.text = $"【{taskName}】任务完成";

            OnBlockingChanged?.Invoke(true);
            Debug.Log($"[Task] 完成弹窗：【{taskName}】任务完成");
        }

        private void HideImmediate()
        {
            if (root != null) root.SetActive(false);
            if (inputBlocker != null) inputBlocker.SetActive(false);
            OnBlockingChanged?.Invoke(false);
        }
    }
}
