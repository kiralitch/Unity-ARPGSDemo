using Cinemachine;
using UnityEngine;
using YooAsset;

/*
 * 锁定标记（屏幕上的瞄准圆心）
 */

public class LockOnMarker : MonoBehaviour
{
    [field: Header("引用")]
    [field: SerializeField]
    [field: Tooltip("玩家的锁定组件。留空会在场景里自动找（CameraLockIn 挂在 PlayerCamera 上）")]
    public CameraLockIn LockIn { get; private set; }

    [field: SerializeField]
    [field: Tooltip("标记实例的父节点。留空则用本物体的父节点，也就是 Canvas")]
    public RectTransform MarkerParent { get; private set; }

    [field: Header("显示")]
    [field: SerializeField]
    [field: Tooltip("标记相对敌人被投影点的屏幕像素偏移，用来压到胸口而不是脚底")]
    public Vector2 ScreenOffset { get; private set; } = new Vector2(0f, 40f);

    [field: SerializeField]
    [field: Tooltip("投影时取敌人身上多高的位置（米）。0 表示用敌人原点，也就是脚底")]
    public float AimHeightOffset { get; private set; } = 0.6f;

    [field: SerializeField]
    [field: Tooltip("目标在相机背后时是否隐藏标记")]
    public bool bHideWhenNotVisible { get; private set; } = true;

    [field: Header("资源")]
    [field: SerializeField]
    [field: Tooltip("需要绘制的图片预制体路径（YooAsset 地址）")]
    public string PerfebPointPath { get; private set; }

    /* 投影用的相机。用 Camera.main 拿到的是带 CinemachineBrain 的主相机 */
    private Camera MainCamera;

    /* 已实例化的标记对象。解锁时要销毁它 */
    private GameObject CachedPointPerfeb;

    /* 标记实例的 RectTransform。实例化时就缓存下来，
       避免每帧 GetComponent，也避免拿到已经不对的节点 */
    private RectTransform CachedPointRect;

    /* 标记实例所在的 Canvas。用它的 scaleFactor 把像素换成 Canvas 局部单位 */
    private Canvas CachedCanvas;

    /* 加载句柄。解锁时要 Release，否则每次锁定都会多占一份资源引用 */
    private AssetOperationHandle CachedHandle;

    /* 是否正在加载中，避免同一帧重复发起加载 */
    private bool bIsLoading;

    /* 上一帧的锁定状态，用来检测「刚锁定」这个边沿 */
    private bool bWasLocked;

    private void Awake()
    {
        if (LockIn == null)
        {
            /* CameraLockIn 和 CinemachineVirtualCamera 一起挂在 PlayerCamera 上，
               这里用场景查找兜底，省得每处都手动拖引用 */
            LockIn = FindObjectOfType<CameraLockIn>();
        }

        if (LockIn == null)
        {
            Debug.LogError($"{name} 找不到 CameraLockIn，请手动指定 LockIn 引用");
            enabled = false;
            return;
        }

        if (MarkerParent == null)
        {
            MarkerParent = transform as RectTransform;
        }

        if (MarkerParent == null)
        {
            Debug.LogError(
                $"{name} 的 MarkerParent 为空，且父节点不是 UI 节点。" +
                "锁定标记实例需要挂到 Canvas 下，请确认本物体挂在 PlayerHUD/Canvas 下面");
            enabled = false;
            return;
        }
    }

    /* CinemachineCore.CameraUpdatedEvent 是 Brain 把状态推给 Unity 相机之后立刻触发的，
    使用订阅事件方法既能准确执行也能减少cpu计算
     */
    private void OnEnable()
    {
        CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
    }

    private void OnDisable()
    {
        CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);
    }

    /* 由 Cinemachine 在相机更新完成后回调，此时相机变换已是本帧最终值 */
    private void OnCameraUpdated(CinemachineBrain brain)
    {
        if (LockIn == null) return;

        bool bLockedNow = LockIn.bIsLocked;

        /* 检测锁定状态的上升沿：刚锁定时加载标记 */
        if (bLockedNow && !bWasLocked)
        {
            LoadPerfabPoint();
        }

        /* 检测下降沿：解锁时销毁标记并释放句柄 */
        if (!bLockedNow && bWasLocked)
        {
            DestroyPointPerfab();
        }

        bWasLocked = bLockedNow;

        if (!bLockedNow) return;

        /* 异步加载还没完成，这一帧没有实例可摆，等下一帧 */
        if (CachedPointPerfeb == null) return;

        DrawThePointPosition();
    }

    /*private void LateUpdate()
    {
        if (LockIn == null) return;

        bool bLockedNow = LockIn.bIsLocked;

        /* 检测锁定状态的上升沿：刚锁定时加载标记 #1#
        if (bLockedNow && !bWasLocked)
        {
            LoadPerfabPoint();
        }

        /* 检测下降沿：解锁时销毁标记并释放句柄 #1#
        if (!bLockedNow && bWasLocked)
        {
            DestroyPointPerfab();
        }

        bWasLocked = bLockedNow;

        if (!bLockedNow) return;

        /* 异步加载还没完成，这一帧没有实例可摆，等下一帧 #1#
        if (CachedPointPerfeb == null) return;

        DrawThePointPosition();
    }*/
    
    /* 将图片绘制屏幕 */
    private void DrawThePointPosition()
    {
        if (LockIn.LockedTarget == null)
        {
            SetVisible(false);
            return;
        }

        if (MainCamera == null) MainCamera = Camera.main;
        if (MainCamera == null || CachedPointRect == null || MarkerParent == null)
        {
            SetVisible(false);
            return;
        }

        Vector3 WorldPoint = LockIn.LockedTarget.transform.position + Vector3.up * AimHeightOffset;

        /* 用相机矩阵直接做投影，不依赖 Camera.WorldToScreenPoint 返回的是0-1的向量 */
        Vector3 ViewportPoint = MainCamera.WorldToViewportPoint(WorldPoint);

        /* z <= 0 说明目标在相机背后，此时投影坐标是镜像后的错误值，必须过滤 */
        if (bHideWhenNotVisible && ViewportPoint.z <= 0f)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        if (ViewportPoint.z <= 0f) return;

        /* 把 0~1 的视口坐标乘以父节点的尺寸，得到父节点局部坐标 */
        Vector2 MarkerSize = MarkerParent.rect.size;
        Vector2 LocalPosition = new Vector2(
            ViewportPoint.x * MarkerSize.x,
            ViewportPoint.y * MarkerSize.y);

        /* ScreenOffset 是像素，需要用 Canvas 的缩放换算成局部单位 */
        float ScaleFactor = CachedCanvas != null && CachedCanvas.scaleFactor > 0f
            ? CachedCanvas.scaleFactor
            : 1f;

        LocalPosition += ScreenOffset / ScaleFactor;

        /* 吸附到整数像素。角色连续移动时投影值每帧只有极小变化，
           浮点误差会让标记在相邻像素之间来回跳，吸附后视觉上就稳定了 */
        LocalPosition.x = Mathf.Round(LocalPosition.x);
        LocalPosition.y = Mathf.Round(LocalPosition.y);

        CachedPointRect.anchoredPosition = LocalPosition;
    }

    /* 加载并实例化锁定标记。已在加载中或已有实例时直接返回 */
    private void LoadPerfabPoint()
    {
        if (bIsLoading) return;
        if (CachedPointPerfeb != null) return;

        if (string.IsNullOrEmpty(PerfebPointPath))
        {
            Debug.LogError($"{name} 的 PerfebPointPath 为空，无法加载锁定标记");
            return;
        }

        bIsLoading = true;

        var handle = YooAssets.LoadAssetAsync<GameObject>(PerfebPointPath);
        handle.Completed += (h) =>
        {
            bIsLoading = false;

            /* 加载期间玩家可能已经解锁了（或又锁了别的目标），
               这时不能再把实例挂上去，直接释放掉这次加载的结果 */
            if (!LockIn.bIsLocked)
            {
                h.Release();
                return;
            }

            if (h.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"{name} 加载锁定标记失败：{PerfebPointPath}");
                h.Release();
                return;
            }

            CachedHandle = h;
            CachedPointPerfeb = h.InstantiateSync(MarkerParent);

            if (CachedPointPerfeb == null)
            {
                Debug.LogError($"{name} 实例化锁定标记失败：{PerfebPointPath}");
                DestroyPointPerfab();
                return;
            }

            CachedPointRect = CachedPointPerfeb.GetComponent<RectTransform>();

            if (CachedPointRect == null)
            {
                Debug.LogError($"{PerfebPointPath} 根节点上没有 RectTransform，无法作为 UI 标记使用");
                DestroyPointPerfab();
                return;
            }

            /* 锚点钉在父节点左下角，这样 anchoredPosition 就是相对左下的像素偏移，
               与父节点尺寸、缩放都无关，是这里最稳定的摆法 */
            CachedPointRect.anchorMin = Vector2.zero;
            CachedPointRect.anchorMax = Vector2.zero;
            CachedPointRect.pivot = new Vector2(0.5f, 0.5f);

            CachedCanvas = CachedPointPerfeb.GetComponentInParent<Canvas>();

            /* 刚实例化的这一帧先藏起来，等 LateUpdate 算出位置再显示，
               避免标记在原点闪一下 */
            SetVisible(false);
        };
    }

    /* 销毁标记实例并释放加载句柄。可重复调用 */
    private void DestroyPointPerfab()
    {
        if (CachedPointPerfeb != null)
        {
            Destroy(CachedPointPerfeb);
            CachedPointPerfeb = null;
        }

        CachedPointRect = null;
        CachedCanvas = null;

        /* Release 会减掉这次加载的引用计数，不释放就会一直占着资源 */
        if (CachedHandle != null)
        {
            CachedHandle.Release();
            CachedHandle = null;
        }
    }

    /* 控制标记显隐 */
    private void SetVisible(bool bVisible)
    {
        if (CachedPointPerfeb == null) return;
        if (CachedPointPerfeb.activeSelf != bVisible) CachedPointPerfeb.SetActive(bVisible);
    }

    /* 对象被禁用/销毁时兜底清理，避免留下泄漏的实例与句柄 */
    private void OnDestroy()
    {
        DestroyPointPerfab();
    }
}
