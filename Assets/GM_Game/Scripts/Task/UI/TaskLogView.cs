using UnityEngine;
using UnityEngine.UI;

/*
 * 任务日志视图（MVC - View）
 *
 * 继承 UIBase，遵循项目现有 MVC 模式。
 * 只负责展示：仅进行中任务，按序列号排序，显示全部目标进度，只滚动。
 * 无分类、无已完成列表、无全部列表。
 *
 * 预制体约定（详见文件末尾 TaskLogViewFactory 的构建说明）：
 * 需要一个 ScrollRect（滚动只读），Content 下用条目预制体填充。
 */

namespace Game.Task.UI
{
    public class TaskLogView : UIBase
    {
        [SerializeField, Header("滚动视图")]
        private ScrollRect scrollView;
        [SerializeField, Header("条目预制体")] 
        private TaskLogItemView itemPrefab;

        public ScrollRect ScrollView => scrollView;
        public TaskLogItemView ItemPrefab => itemPrefab;

        /* 清空滚动内容 */
        public void ClearItems()
        {
            if (scrollView == null || scrollView.content == null) return;

            var content = scrollView.content;
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Destroy(content.GetChild(i).gameObject);
            }
        }

        public TaskLogItemView CreateItem()
        {
            if (itemPrefab == null || scrollView == null || scrollView.content == null) return null;
            return Instantiate(itemPrefab, scrollView.content);
        }

        public void SetScrollActive(bool active)
        {
            if (scrollView != null) scrollView.gameObject.SetActive(active);
            else gameObject.SetActive(active);
        }
    }
}
