using System;
using Game.Task.Events;

/*
 * 全局游戏事件总线（本地单机逻辑使用）
 *
 * 说明：项目现有 SocketDispatcher 只处理网络协议事件，没有本地通用事件总线，
 * 因此这里提供最小实现。任务系统仅通过该总线接收怪物死亡事件，不在 Update 每帧轮询。
 *
 * TODO 若后续需要背包等其他事件，可在此扩展更多事件类型（当前已预留 ItemObtained 通道）。
 */

namespace Game.Task.Events
{
    public static class GameEventBus
    {
        /// <summary>怪物死亡事件：monsterId + killer。</summary>
        public static event Action<MonsterDiedEvent> OnMonsterDied;

        /// <summary>发布怪物死亡事件（由战斗系统调用）。</summary>
        public static void RaiseMonsterDied(MonsterDiedEvent evt)
        {
            OnMonsterDied?.Invoke(evt);
        }

        // ---- 预留：背包获取事件（要求 21，当前不接收集目标） ----

        /// <summary>TODO 预留：物品获取事件。收到后需要由 TaskManager 转发给收集目标。</summary>
        public static event Action<int, int> OnItemObtained;

        /// <summary>TODO 预留：发布物品获取事件。</summary>
        public static void RaiseItemObtained(int itemId, int count)
        {
            OnItemObtained?.Invoke(itemId, count);
        }
    }
}
