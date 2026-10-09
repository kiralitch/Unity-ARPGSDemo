using Game.Task.Core;

/*
 * 任务完成弹窗控制器（MVC - Controller）
 *
 * 订阅 TaskManager.OnTaskCompleted，将完成任务名称送入弹窗队列。
 * 弹窗显示期间阻塞玩家输入（通过 Player.SetUIInputAllMethon）。
 */

namespace Game.Task.UI
{
    public class CompletionPopupController : CTRL_Base
    {
        private readonly CompletionPopupView _view;

        public CompletionPopupController(UIBase view) : base(view)
        {
            _view = view as CompletionPopupView;
            if (_view != null)
            {
                _view.OnBlockingChanged += HandleBlockingChanged;
            }
        }

        public void Bind()
        {
            if (TaskManager.Instance == null) return;
            TaskManager.Instance.OnTaskCompleted -= OnTaskCompleted;
            TaskManager.Instance.OnTaskCompleted += OnTaskCompleted;
        }

        public void Unbind()
        {
            if (TaskManager.Instance == null) return;
            TaskManager.Instance.OnTaskCompleted -= OnTaskCompleted;
            if (_view != null) _view.OnBlockingChanged -= HandleBlockingChanged;
        }

        private void OnTaskCompleted(string taskName)
        {
            _view?.Enqueue(taskName);
        }

        /* 弹窗显示期间暂停玩家输入 */
        private void HandleBlockingChanged(bool blocking)
        {
            if (Player.Instance == null) return;
            Player.Instance.SetUIInputAllMethon(!blocking);
        }
    }
}
