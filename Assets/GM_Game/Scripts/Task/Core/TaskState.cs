/*
 * 任务状态
 *
 * 最终只有三种状态：
 * - InPool   ：未接（在池中）
 * - InProgress：进行中（“已接”与“进行中”合并）
 * - Completed：已完成（瞬时状态，完成回调后立即从日志移除并回池）
 */

namespace Game.Task.Core
{
    public enum TaskState
    {
        InPool = 0,      //未接（在池中）
        InProgress = 1,  //进行中
        Completed = 2,   //已完成（瞬时，仅用于完成回调期间）
    }
}
