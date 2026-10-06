using System.Collections.Generic;
using UnityEngine;

/*
 * 单个对象池
 *
 * 负责一种预制体的实例复用：维护空闲实例列表，取出时复用或新建，
 * 回收时停用并放回列表；同时负责该池内所有正在使用对象的自动回收倒计时。
 *
 * 由 ObjectPoolManager 统一创建与驱动，一般不直接 new 出来用。
 */

public class ObjectPool
{
    /* 池对应的预制体（作为池的身份标识） */
    public GameObject Prefab { get; private set; }

    /* 池名，仅用于 Hierarch面 中归类显示，方便调试 */
    public string PoolName { get; private set; }

    /* 池的父节点：所有本池实例长期挂在这个节点下，便于层级整洁与统一管理 */
    private Transform PoolRoot;

    /* 已回收、可复用的空实例 */
    private readonly Stack<PoolItem> IdleItems = new Stack<PoolItem>();

    /* 所有实例（含使用中与空闲），用于统一 Tick 与调试统计 */
    private readonly List<PoolItem> AllItems = new List<PoolItem>();

    /* 单池容量上限：<= 0 表示不限制。超出后回收的实例会被真正销毁，避免无限膨胀 */
    public int MaxSize { get; set; } = 0;

    public ObjectPool(GameObject prefab, string poolName, Transform poolRoot, int prewarmCount = 0, int maxSize = 0)
    {
        Prefab = prefab;
        PoolName = poolName;
        PoolRoot = poolRoot;
        MaxSize = maxSize;

        Prewarm(prewarmCount);
    }

    /* 预热：提前创建 count 个实例放入空闲列表 */
    public void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            PoolItem item = CreateItem();
            Deactivate(item);
            IdleItems.Push(item);
        }
    }

    /* 从池中取出一个实例。
       autoRecycleAfter > 0 时，该实例会在该秒数后被池自动回收（用于特效等）。 */
    public GameObject Get(Transform parent = null, float autoRecycleAfter = 0f)
    {
        PoolItem item = IdleItems.Count > 0 ? IdleItems.Pop() : CreateItem();

        GameObject go = item.Instance;

        /* 挂到调用者指定的父节点（不指定就留在池节点下），并按需复位变换 */
        if (parent != null && go.transform.parent != parent)
        {
            go.transform.SetParent(parent, false);
        }

        go.SetActive(true);
        item.MarkUsed(autoRecycleAfter);

        /* 通知对象自身做「取出」初始化 */
        NotifyGet(go);

        return go;
    }

    /* 从池中取出并直接取组件（常用：Get<Enemy>()） */
    public T Get<T>(Transform parent = null, float autoRecycleAfter = 0f) where T : Component
    {
        GameObject go = Get(parent, autoRecycleAfter);
        return go != null ? go.GetComponent<T>() : null;
    }

    /* 回收一个实例。
       传入的可以是实例上的任意组件（PooledObject / Enemy 等），内部会归一到 PoolItem。 */
    public void Return(Component component)
    {
        if (component == null) return;
        Return(component.gameObject);
    }

    public void Return(GameObject go)
    {
        if (go == null) return;

        PoolItem item = FindItem(go);
        if (item == null)
        {
            /* 不是本池的对象：直接销毁，避免误用导致泄漏 */
            Object.Destroy(go);
            return;
        }

        if (!item.bInUse) return; //重复回收保护

        /* 通知对象自身做「回收」清理 */
        NotifyReturn(go);

        /* 取消自动回收计时，回到池节点下并停用 */
        item.MarkReturned();

        if (PoolRoot != null)
        {
            go.transform.SetParent(PoolRoot, false);
        }

        go.SetActive(false);

        /* 超过容量上限就直接销毁，不留在池里占内存 */
        if (MaxSize > 0 && IdleItems.Count >= MaxSize)
        {
            AllItems.Remove(item);
            Object.Destroy(go);
            return;
        }

        IdleItems.Push(item);
    }

    /* 回收本池所有使用中的实例（切场景 / 清场时调用） */
    public void ReturnAll()
    {
        /* 收集后再回收：Return 会改动 AllItems，先快照避免遍历中修改 */
        PoolItem[] snapshot = AllItems.ToArray();

        for (int i = 0; i < snapshot.Length; i++)
        {
            if (snapshot[i].bInUse) Return(snapshot[i].Instance);
        }
    }

    /* 销毁本池全部实例并清空（真正释放，不可再复用） */
    public void Dispose()
    {
        for (int i = 0; i < AllItems.Count; i++)
        {
            if (AllItems[i].Instance != null)
            {
                Object.Destroy(AllItems[i].Instance);
            }
        }

        AllItems.Clear();
        IdleItems.Clear();
    }

    /* 每帧驱动本池所有使用中对象的自动回收倒计时 */
    public void Tick(float deltaTime)
    {
        /* 倒计时到点会调 Return 改动 AllItems，先快照避免遍历中修改 */
        PoolItem[] snapshot = AllItems.ToArray();

        for (int i = 0; i < snapshot.Length; i++)
        {
            PoolItem item = snapshot[i];

            if (item.TickAutoRecycle(deltaTime))
            {
                Return(item.Instance);
            }
        }
    }

    /* 调试统计 */
    public int IdleCount => IdleItems.Count;
    public int TotalCount => AllItems.Count;
    public int ActiveCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < AllItems.Count; i++)
            {
                if (AllItems[i].bInUse) count++;
            }
            return count;
        }
    }

    #region 内部工具

    private PoolItem CreateItem()
    {
        GameObject go = Object.Instantiate(Prefab, PoolRoot);

        PoolItem item = new PoolItem(go, this);
        AllItems.Add(item);

        /* 注入池归属，让实例能通过 PooledObject.ReturnToPool() 主动还池。
           预制体没挂 PooledObject 时自动补一个，保证任何预制体都能被池管理 */
        PooledObject pooled = go.GetComponent<PooledObject>();
        if (pooled == null)
        {
            pooled = go.AddComponent<PooledObject>();
        }
        pooled.OwnerPool = this;

        return item;
    }

    private void Deactivate(PoolItem item)
    {
        item.MarkReturned();
        if (item.Instance != null) item.Instance.SetActive(false);
    }

    /* 判断某个实例是否由本池管理 */
    public bool Contains(GameObject go)
    {
        return FindItem(go) != null;
    }

    private PoolItem FindItem(GameObject go)
    {
        for (int i = 0; i < AllItems.Count; i++)
        {
            if (AllItems[i].Instance == go) return AllItems[i];
        }

        return null;
    }

    private void NotifyGet(GameObject go)
    {
        /* 实例上可能有多个 IPoolable，全部通知一遍 */
        IPoolable[] poolables = go.GetComponentsInChildren<IPoolable>(true);
        for (int i = 0; i < poolables.Length; i++)
        {
            poolables[i].OnPoolGet();
        }
    }

    private void NotifyReturn(GameObject go)
    {
        IPoolable[] poolables = go.GetComponentsInChildren<IPoolable>(true);
        for (int i = 0; i < poolables.Length; i++)
        {
            poolables[i].OnPoolReturn();
        }
    }

    #endregion
}
