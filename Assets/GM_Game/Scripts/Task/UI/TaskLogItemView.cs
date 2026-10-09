using System.Text;
using Game.Task.Config;
using Game.Task.Core;
using TMPro;
using UnityEngine;

/*
 * 任务日志单条视图
 *
 * 显示：任务名称 + 全部目标进度（例如“史莱姆 3/5”）。
 */

namespace Game.Task.UI
{
    public class TaskLogItemView : MonoBehaviour
    {
        [SerializeField, Header("任务名称")] 
        private TMP_Text titleText;
        [SerializeField, Header("目标进度（多行）")] 
        private TMP_Text objectiveText;

        /// <summary>用任务实例刷新此条目。</summary>
        public void Refresh(TaskInstance instance, TaskDatabase database)
        {
            if (instance == null) return;

            var def = database != null ? database.Get(instance.TaskId) : null;

            if (titleText != null)
            {
                string name = def != null ? def.TaskName : instance.TaskId;
                titleText.text = name;
            }

            if (objectiveText != null)
            {
                var sb = new StringBuilder();
                foreach (var o in instance.Objectives)
                {
                    if (sb.Length > 0) sb.Append('\n');
                    sb.Append(FormatObjective(o));
                }
                objectiveText.text = sb.ToString();
            }
        }

        /* 击杀类型显示 “击杀 史莱姆 3/5”：名称来自配置的 TargetName，怪物 ID 不显示 */
        private string FormatObjective(TaskObjectiveRuntime o)
        {
            return $"{FormatType(o.Type)} {o.DisplayName} {o.CurrentCount}/{o.RequiredCount}";
        }

        private string FormatType(TaskObjectiveType type)
        {
            switch (type)
            {
                case TaskObjectiveType.Kill: return "击杀";
                case TaskObjectiveType.Collect: return "收集";
                case TaskObjectiveType.Interact: return "交互";
                case TaskObjectiveType.Reach: return "到达";
                default: return type.ToString();
            }
        }
    }
}
