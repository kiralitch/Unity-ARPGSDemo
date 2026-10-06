using System.Collections.Generic;
using UnityEngine;
using YooAsset;

/*
 * 对象池统一管理器
 *   1. 统一管理多个对象池（按预制体 / 路径划分），提供取出与回收的统一入口；
 *   2. 每帧驱动所有池的定时器，实现「到点自动回收」；
 *   3. 处理 YooAsset 异步加载后再取池的流程。
 *
 * 使用方式：
 *   ObjectPoolManager.Instance.Get(prefab);                       // 直接取
 *   ObjectPoolManager.Instance.Get(prefab, parent, autoRecycle);  // 指定父节点 + 自动回收秒数
 *   ObjectPoolManager.Instance.Return(go);                        // 回收
 *   ObjectPoolManager.Instance.Prewarm(prefab, 10);               // 预热
 *
 * 敌人 / 特效 / 远程攻击 / 可交互物都用同一套接口
 * 具体对象只要按需实现 IPoolable 或挂 PooledObject 即可
 */

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    /* 所有池实例的父节点，保持层级整洁 */
    private Transform PoolRoot;

    /* 以预制体为键的池注册表 */
    private readonly Dictionary<GameObject, ObjectPool> Pools = new Dictionary<GameObject, ObjectPool>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        PoolRoot = new GameObject("[ObjectPool]").transform;
        PoolRoot.SetParent(transform, false);
    }

    /* 场景里没手动挂时兜底创建，保证 Instance 一定可用 */
    public static ObjectPoolManager EnsureInstance()
    {
        if (Instance != null) return Instance;

        GameObject go = new GameObject("[ObjectPoolManager]");
        return go.AddComponent<ObjectPoolManager>();
    }

    private void Update()
    {
        /* 驱动所有池的自动回收计时器。
           遍历中一般不会增删池（池是惰性创建的），但为稳妥仍做快照 */
        if (Pools.Count == 0) return;

        foreach (ObjectPool pool in Pools.Values)
        {
            pool.Tick(Time.deltaTime);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    #region 池的创建 / 获取

    /* 取得（不存在则创建）某个预制体对应的池 */
    public ObjectPool GetOrCreatePool(GameObject prefab, int prewarmCount = 0, int maxSize = 0)
    {
        if (prefab == null)
        {
            Debug.LogError("[ObjectPoolManager] 预制体为空，无法创建对象池");
            return null;
        }

        if (Pools.TryGetValue(prefab, out ObjectPool pool))
        {
            /* 已存在：允许后调用的 maxSize 覆盖，便于运行时调整上限 */
            if (maxSize > 0) pool.MaxSize = maxSize;
            return pool;
        }

        /* 每个池一个子节点，方便在 Hierarchy 里分类查看空闲对象 */
        Transform poolRoot = new GameObject(prefab.name + "_Pool").transform;
        poolRoot.SetParent(PoolRoot, false);

        pool = new ObjectPool(prefab, prefab.name, poolRoot, prewarmCount, maxSize);
        Pools.Add(prefab, pool);

        return pool;
    }

    /* 预热指定预制体，提前生成 count 个实例 */
    public void Prewarm(GameObject prefab, int count, int maxSize = 0)
    {
        GetOrCreatePool(prefab, count, maxSize);
    }

    #endregion

    #region 取出

    /* 从池中取出一个实例 */
    public GameObject Get(GameObject prefab, Transform parent = null, float autoRecycleAfter = 0f)
    {
        ObjectPool pool = GetOrCreatePool(prefab);
        return pool != null ? pool.Get(parent, autoRecycleAfter) : null;
    }

    /* 取出并直接拿组件（常用：Get<Enemy>(enemyPrefab)） */
    public T Get<T>(GameObject prefab, Transform parent = null, float autoRecycleAfter = 0f) where T : Component
    {
        ObjectPool pool = GetOrCreatePool(prefab);
        return pool != null ? pool.Get<T>(parent, autoRecycleAfter) : null;
    }

    /* 带位置与朝向的取出（敌人生成、特效摆放常用） */
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, float autoRecycleAfter = 0f)
    {
        GameObject go = Get(prefab, parent, autoRecycleAfter);
        if (go == null) return null;

        go.transform.SetPositionAndRotation(position, rotation);
        return go;
    }
    
    public T Get<T>(GameObject prefab, Vector3 position, Quaternion rotation,
        Transform parent = null, float autoRecycleAfter = 0f) where T : Component
    {
        GameObject go = Get(prefab, position, rotation, parent, autoRecycleAfter);
        return go != null ? go.GetComponent<T>() : null;
    }

    /* 通过 YooAsset 路径异步取出
       先加载预制体（走 ResourceManager 缓存），成功后再从池里取，回调返回实例。 */
    public void GetAsync(string assetPath, Transform parent, System.Action<GameObject> callback, float autoRecycleAfter = 0f)
    {
        ResourceManager.GetInstance.LoadPrefabAsync(assetPath, (GameObject prefab) =>
        {
            if (prefab == null)
            {
                Debug.LogError($"[ObjectPoolManager] 加载预制体失败：{assetPath}");
                callback?.Invoke(null);
                return;
            }

            GameObject go = Get(prefab, parent, autoRecycleAfter);
            callback?.Invoke(go);
        });
    }

    #endregion

    #region 回收

    /* 回收实例（传 GameObject 或其任意组件都可） */
    public void Return(GameObject go)
    {
        if (go == null) return;

        /* 优先用实例上记录的池归属，最准也最快 */
        PooledObject pooled = go.GetComponent<PooledObject>();
        if (pooled != null && pooled.OwnerPool != null)
        {
            pooled.OwnerPool.Return(go);
            return;
        }

        /* 退而求其次：按所属池查找（遍历所有池） */
        foreach (ObjectPool pool in Pools.Values)
        {
            if (pool.Contains(go))
            {
                pool.Return(go);
                return;
            }
        }

        /* 完全不属于任何池：直接销毁，避免泄漏 */
        Debug.LogWarning($"[ObjectPoolManager] {go.name} 不属于任何对象池，已直接销毁");
        Destroy(go);
    }

    public void Return(Component component)
    {
        if (component == null) return;
        Return(component.gameObject);
    }

    /* 回收某预制体池里的全部使用中对象 */
    public void ReturnAll(GameObject prefab)
    {
        if (prefab != null && Pools.TryGetValue(prefab, out ObjectPool pool))
        {
            pool.ReturnAll();
        }
    }

    /* 回收所有池的所有使用中对象（清场 / 切场景） */
    public void ReturnAll()
    {
        foreach (ObjectPool pool in Pools.Values)
        {
            pool.ReturnAll();
        }
    }

    #endregion

    #region 清理

    /* 销毁某个池（含其全部实例） */
    public void DisposePool(GameObject prefab)
    {
        if (prefab == null) return;

        if (Pools.TryGetValue(prefab, out ObjectPool pool))
        {
            pool.Dispose();
            Pools.Remove(prefab);
        }
    }

    /* 销毁所有池与全部实例 */
    public void DisposeAll()
    {
        foreach (ObjectPool pool in Pools.Values)
        {
            pool.Dispose();
        }

        Pools.Clear();
    }

    #endregion
}
