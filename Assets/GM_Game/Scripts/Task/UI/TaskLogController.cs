using Game.Task.Core;

/*
 * 任务日志控制器（MVC - Controller）
 *
 * 继承项目现有 CTRL_Base。
 * 订阅 TaskManager 的日志变化事件（事件驱动，非每帧刷新），重建日志列表。
 */

namespace Game.Task.UI
{
    public class TaskLogController : CTRL_Base
    {
        private readonly TaskLogView _view;

        public TaskLogController(UIBase view) : base(view)
        {
            _view = view as TaskLogView;
            if (_view != null) _view.InitView();
        }

        /* 由外部在合适的时机调用，开始接收刷新事件 */
        public void Bind()
        {
            if (TaskManager.Instance == null) return;
            TaskManager.Instance.OnTaskLogChanged -= Refresh;
            TaskManager.Instance.OnTaskLogChanged += Refresh;
            Refresh();
        }

        public void Unbind()
        {
            if (TaskManager.Instance == null) return;
            TaskManager.Instance.OnTaskLogChanged -= Refresh;
        }

        public override void ShowView()
        {
            base.ShowView();
            Bind();
            _view?.Show(true);
        }

        public override void HideView()
        {
            base.HideView();
            _view?.Show(false);
        }

        /* 重建日志列表：仅进行中任务，按序列号排序 */
        public void Refresh()
        {
            if (_view == null) return;

            _view.ClearItems();

            var manager = TaskManager.Instance;
            if (manager == null) return;

            var tasks = manager.GetSortedLogTasks();
            foreach (var inst in tasks)
            {
                var item = _view.CreateItem();
                if (item != null) item.Refresh(inst, manager.Database);
            }
        }
    }
}
