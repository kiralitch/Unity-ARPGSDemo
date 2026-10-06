using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

/*
 * 统一管理敌人对象池类
 *
 * 职责：
 *   1. 预加载敌人预制体并预热对象池；
 *   2. 缓存「路径 -> 预制体」，这样外部只用路径就能生成敌人
 *      （调用方不需要自己拿预制体引用）；
 *   3. 提供统一的敌人生成入口。
 */

public class GameEnemyManager : MonoBehaviour
{
    public List<string> EnemyPathList = new List<string>();

    public static GameEnemyManager Instance { get; private set; }

    private ObjectPoolManager EnemyPool;

    /* 路径 -> 预制体 的缓存，预热时写入。
       有了它，外部就能用路径直接 SpawnEnemy，不必持有预制体引用。
       键统一做小写归一化，避免大小写差异导致查不到 */
    private static readonly Dictionary<string, GameObject> PrefabCache = new Dictionary<string, GameObject>();

    private Transform _enemyRoot;

    public Transform EnemyRoot
    {
        set
        {
            if (value != null) _enemyRoot = value;
        }
        get
        {
            if (_enemyRoot == null)
            {
                GameObject root = GameObject.Find("Enemy");
                _enemyRoot = root != null ? root.transform : new GameObject("Enemy").transform;
            }
            return _enemyRoot;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        /* 兜底拿对象池管理器，避免 Awake 顺序导致 Instance 还没赋值 */
        EnemyPool = ObjectPoolManager.EnsureInstance();
    }

    private void Start()
    {
        StartCoroutine(StartPreLoadPathList());
    }

    private IEnumerator StartPreLoadPathList()
    {
        yield return new WaitForSeconds(0.5f);

        PreloadPathList();
    }

    /* 预加载敌人路径列表 */
    private void PreloadPathList()
    {
        if (EnemyPathList == null) return;

        foreach (var prefabPath in EnemyPathList)
        {
            if (string.IsNullOrEmpty(prefabPath)) continue;

            PreLoadEnemyPrefab(prefabPath);
        }
    }

    /*
     * 预加载敌人预制体：加载 -> 写入路径缓存 -> 预热对象池
     */
    public static void PreLoadEnemyPrefab(string prefabPath, int prewarmCount = 3, int maxSize = 0)
    {
        if (string.IsNullOrEmpty(prefabPath)) return;

        /* 敌人是 GameObject 预制体，泛型必须用 GameObject */
        var handle = YooAssets.LoadAssetAsync<GameObject>(prefabPath);
        handle.Completed += (h) =>
        {
            if (h.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"[GameEnemyManager] 预加载失败：{prefabPath}，{h.LastError}");
                return;
            }

            GameObject prefab = h.AssetObject as GameObject;
            if (prefab == null)
            {
                Debug.LogError($"[GameEnemyManager] 资源不是 GameObject：{prefabPath}");
                return;
            }

            /* 缓存路径 -> 预制体，供后续按路径生成使用 */
            CachePrefab(prefabPath, prefab);

            ObjectPoolManager.EnsureInstance().Prewarm(prefab, prewarmCount, maxSize);
            Debug.Log($"[GameEnemyManager] 已预热 {prefab.name} x{prewarmCount}");
        };
    }

    /*
     * 按路径生成敌人（推荐给触发器使用）。
     * 位置由 position 决定，父节点挂到 EnemyRoot 下，与其他敌人同级。
     */
    public Enemy SpawnEnemy(string prefabPath, Vector3 position, Quaternion rotation)
    {
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogWarning("[GameEnemyManager] 生成敌人失败：路径为空");
            return null;
        }

        if (!TryGetCachedPrefab(prefabPath, out GameObject prefab))
        {
            /* 还没预加载到：抛提示，调用方应确保先 PreLoadEnemyPrefab */
            Debug.LogWarning($"[GameEnemyManager] 生成敌人失败：{prefabPath} 尚未预加载");
            return null;
        }

        return SpawnEnemy(prefab, position, rotation);
    }

    /* 直接用预制体生成敌人 */
    public Enemy SpawnEnemy(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[GameEnemyManager] 生成敌人失败：预制体为空");
            return null;
        }

        Enemy enemy = ObjectPoolManager.EnsureInstance()
            .Get<Enemy>(prefab, position, rotation, EnemyRoot);

        if (enemy == null)
        {
            Debug.LogWarning($"[GameEnemyManager] 生成敌人失败：{prefab.name}");
        }

        return enemy;
    }

    /* 在指定位置生成（朝向用默认） */
    public Enemy SpawnEnemy(string prefabPath, Vector3 position)
    {
        return SpawnEnemy(prefabPath, position, Quaternion.identity);
    }

    #region 路径缓存

    private static string NormalizePath(string path)
    {
        return string.IsNullOrEmpty(path) ? string.Empty : path.ToLowerInvariant();
    }

    private static void CachePrefab(string path, GameObject prefab)
    {
        PrefabCache[NormalizePath(path)] = prefab;
    }

    private static bool TryGetCachedPrefab(string path, out GameObject prefab)
    {
        return PrefabCache.TryGetValue(NormalizePath(path), out prefab);
    }

    #endregion
}
