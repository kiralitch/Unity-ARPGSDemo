using Game.Task.Config;

/*
 * 任务目标的运行时进度
 */

namespace Game.Task.Core
{
    public class TaskObjectiveRuntime
    {
        public TaskObjectiveType Type;   //目标类型（当前仅 Kill）
        public int TargetId;             //目标 ID（怪物 ID，仅用于监听匹配，不显示在 UI）
        public string TargetName;        //目标显示名称（UI 显示用，例如“史莱姆”）
        public int RequiredCount;        //需要数量
        public int CurrentCount;         //当前计数

        public bool IsCompleted => CurrentCount >= RequiredCount;

        public TaskObjectiveRuntime() { }

        public TaskObjectiveRuntime(TaskObjectiveDefinition def)
        {
            if (def == null) return;
            Type = def.Type;
            TargetId = def.TargetId;
            TargetName = def.TargetName;
            RequiredCount = def.RequiredCount;
            CurrentCount = 0;
        }

        /*
         * 增加进度，返回这次增加后是否发生变化。
         * 计数上限为 RequiredCount（超出不再增加）。
         */
        public bool AddProgress(int delta)
        {
            if (delta <= 0) return false;
            if (CurrentCount >= RequiredCount) return false;

            int before = CurrentCount;
            CurrentCount += delta;
            if (CurrentCount > RequiredCount) CurrentCount = RequiredCount;

            return CurrentCount != before;
        }

        /* 与最新配置重绑：按相同 targetId 保留进度，其余以新配置为准（要求 12） */
        public void Rebind(TaskObjectiveDefinition def)
        {
            if (def == null) return;

            Type = def.Type;
            TargetId = def.TargetId;
            TargetName = def.TargetName;

            // 数量变化时，进度按新上限裁剪；进度本身保留
            RequiredCount = def.RequiredCount;
            if (CurrentCount > RequiredCount) CurrentCount = RequiredCount;
        }

        /* 显示名称：优先用配置的名称，未配置时回退为“目标ID”以保证不显示数字 ID 为空 */
        public string DisplayName => string.IsNullOrEmpty(TargetName) ? $"目标{TargetId}" : TargetName;

        /* 显示文本：如 “史莱姆 3/5” 中的 “3/5” 部分由 UI 拼装 */
        public string ProgressText => $"{CurrentCount}/{RequiredCount}";
    }
}
