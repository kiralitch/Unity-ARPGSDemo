using UnityEngine;

/*
 * 池中对象实例的包装
 *
 * 一个 PoolItem 对应一个被池管理的 GameObject 实例，记录它的激活状态、
 * 自动回收倒计时等运行时信息。池内部通过它来统计与调度，
 * 外部一般只接触 GameObject 本身，不需要直接引用 PoolItem。
 */

public class PoolItem
{
    /* 被池管理的实例（池创建后不再改变） */
    public GameObject Instance { get; private set; }

    /* 对应的池（用于还池时定位） */
    public ObjectPool OwnerPool { get; private set; }

    /* 是否正在使用中（已取出、未回收）。池据此外放/复用 */
    public bool bInUse { get; internal set; }

    /* 自动回收倒计时：> 0 表示还有多少秒自动还池；<= 0 表示不自动回收 */
    public float AutoRecycleCountdown { get; internal set; }

    public PoolItem(GameObject instance, ObjectPool ownerPool)
    {
        Instance = instance;
        OwnerPool = ownerPool;
        bInUse = false;
        AutoRecycleCountdown = 0f;
    }

    /* 取出：标记使用中并启动自动回收倒计时（<=0 表示不自动回收） */
    internal void MarkUsed(float autoRecycleAfter)
    {
        bInUse = true;
        AutoRecycleCountdown = autoRecycleAfter;
    }

    /* 回收：清空使用标记与倒计时 */
    internal void MarkReturned()
    {
        bInUse = false;
        AutoRecycleCountdown = 0f;
    }

    /* 每帧推进倒计时，返回是否到点需要自动回收 */
    internal bool TickAutoRecycle(float deltaTime)
    {
        if (!bInUse) return false;
        if (AutoRecycleCountdown <= 0f) return false;

        AutoRecycleCountdown -= deltaTime;

        return AutoRecycleCountdown <= 0f;
    }
}
