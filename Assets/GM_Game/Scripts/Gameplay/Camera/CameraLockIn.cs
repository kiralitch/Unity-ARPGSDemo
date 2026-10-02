using Cinemachine;
using UnityEngine;

/*
 * 玩家锁定敌人类
 *
 * 只负责「让摄像头看向被锁定的敌人」，不改玩家的移动与攻击朝向逻辑。
 */

public class CameraLockIn : MonoBehaviour
{
    [field: Header("锁定搜索")]
    [field: SerializeField][field: Range(0.5f, 60f)]
    [field: Tooltip("锁定搜索半径，超出这个距离的敌人不会被锁定")]
    public float LockRange { get; private set; } = 15f;

    [field: SerializeField][field: Range(0f, 360f)]
    [field: Tooltip("以相机正前方为中轴的搜索锥全角")]
    public float LockSearchAngle { get; private set; } = 120f;

    [field: Header("锁定表现")]
    [field: SerializeField][field: Range(0.5f, 30f)]
    [field: Tooltip("锁定期间视角转向敌人的速度")]
    public float LockRotateSpeed { get; private set; } = 4f;

    [field: SerializeField]
    [field: Tooltip("看向敌人的高度偏移")]
    public float AimHeightOffset { get; private set; } = 0.55f;

    [field: SerializeField][field: Range(0f, 2f)]
    [field: Tooltip("锁定期间把观察支点抬高的量")]
    public float PivotLift { get; private set; } = 0.45f;

    [field: Header("解锁条件")]
    [field: SerializeField][field: Range(0.5f, 60f)]
    [field: Tooltip("锁定期间目标超出这个距离自动解锁")]
    public float BreakLockDistance { get; private set; } = 20f;

    [field: SerializeField]
    [field: Tooltip("敌人之间是否用射线做遮挡检测")]
    public bool bCheckObstacle { get; private set; } = true;

    [field: SerializeField]
    [field: Tooltip("遮挡检测使用的层")]
    public LayerMask ObstacleMask { get; private set; }

    /* 被锁定的敌人。敌人死亡被销毁后 Unity 会把它判为空，用于自动解锁 */
    private Enemy LockedEnemy;

    /* 当前是否处于锁定状态。玩家 HUD / 其他系统可以据此切换锁定标记 */
    public bool bIsLocked => LockedEnemy != null;

    /* 当前锁定的敌人，未锁定时为 null */
    public Enemy LockedTarget => LockedEnemy;

    private CinemachineVirtualCamera VirtualCamera;

    /* PlayerCamera 上的瞄准组件。锁定期间由本脚本驱动它的角度 */
    private CinemachinePOV PovComponent;

    /* 相机位置组件。锁定期间用它抬高观察支点 */
    private CinemachineFramingTransposer FramingTransposer;

    /* 锁定前 POV 的鼠标输入轴名，解锁时原样还原。
       之所以要清空轴名，是因为 CinemachinePOV 每帧都会用输入轴的值去累加角度，
       只要轴还开着，鼠标就会和锁定方向互相拉扯 */
    private string DefaultHorizontalAxisName;
    private string DefaultVerticalAxisName;
    private bool bHasCachedPovInput;

    /* 锁定前的观察支点偏移，解锁时还原 */
    private Vector3 DefaultTrackedObjectOffset;
    private bool bHasCachedPivot;

    private void Awake()
    {
        VirtualCamera = GetComponent<CinemachineVirtualCamera>();

        if (VirtualCamera == null)
        {
            Debug.LogError($"{name} 上的 CameraLockIn 需要和 CinemachineVirtualCamera 挂在同一个物体上");
            enabled = false;
            return;
        }

        /* POV 挂在 vcam 自动生成的 cm 子物体上，不在 PlayerCamera 本身 */
        PovComponent = VirtualCamera.GetCinemachineComponent<CinemachinePOV>();

        if (PovComponent == null)
        {
            Debug.LogError(
                $"{name} 上没有找到 CinemachinePOV，无法实现锁定。" +
                "锁定依赖 POV 来控制相机朝向，请确认 PlayerCamera 的 Aim 组件是 POV");
            enabled = false;
            return;
        }

        FramingTransposer = VirtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();

        CachePovInput();
        CachePivot();
    }

    /* 记录 POV 原本的输入轴名，锁定结束后要还回去 */
    private void CachePovInput()
    {
        if (bHasCachedPovInput) return;

        DefaultHorizontalAxisName = PovComponent.m_HorizontalAxis.m_InputAxisName;
        DefaultVerticalAxisName = PovComponent.m_VerticalAxis.m_InputAxisName;
        bHasCachedPovInput = true;
    }

    /* 记录观察支点原本的偏移，锁定结束后要还回去 */
    private void CachePivot()
    {
        if (bHasCachedPivot) return;
        if (FramingTransposer == null) return;

        DefaultTrackedObjectOffset = FramingTransposer.m_TrackedObjectOffset;
        bHasCachedPivot = true;
    }

    private void Update()
    {
        if (!bIsLocked) return;

        /* 自动解锁：目标被销毁、目标已死、目标超出解锁距离 */
        if (ShouldBreakLock())
        {
            Unlock();
            Player.Instance.SetLockInCamera(bIsLocked);
            return;
        }

        RotatePovTowardsTarget();
    }

    private bool ShouldBreakLock()
    {
        /* 敌人死亡后会走 DestroyOnDeath 销毁 GameObject，
           这里 Unity 的假空判断会直接命中，必须先判 null 再读血量 */
        if (LockedEnemy == null) return true;

        if (!LockedEnemy.RuntimeCommonData.CheckIsHaveHealth()) return true;
        
        float Distance = Vector3.Distance(transform.position, LockedEnemy.transform.position);

        return Distance > BreakLockDistance;
    }

    /* 插值设置Pov角度
       不直接赋值而是插值，否则锁定/解锁瞬间视角会瞬移 */
    private void RotatePovTowardsTarget()
    {
        Vector3 AimPoint = LockedEnemy.transform.position + Vector3.up * AimHeightOffset;

        /* 用相机当前位置算方向。POV 的角度就是相机自身的朝向，
           所以这里必须用相机的实际位置，不能用玩家位置 */
        Vector3 Direction = AimPoint - transform.position;
        if (Direction.sqrMagnitude < 0.0001f) return;

        float TargetYaw = Mathf.Atan2(Direction.x, Direction.z) * Mathf.Rad2Deg;
        float HorizontalDistance = new Vector2(Direction.x, Direction.z).magnitude;

        /* VerticalAxis.Value 直接对应相机的 X 旋转（POV 内部就是
           Quaternion.Euler(VerticalAxis.Value, HorizontalAxis.Value, 0)），
           所以正值为俯视、负值为仰视。m_InvertInput 只作用于鼠标输入，
           不会作用到 Value 本身，这里不能再翻转。
           目标在相机上方时 Direction.y > 0，取负号得到负角度，即向上仰视 */
        float TargetPitch = -Mathf.Atan2(Direction.y, HorizontalDistance) * Mathf.Rad2Deg;

        float T = 1f - Mathf.Exp(-LockRotateSpeed * Time.deltaTime);

        /* 水平方向走最短路径，避免在 0/360 交界处绕一整圈 */
        float CurrentYaw = PovComponent.m_HorizontalAxis.Value;
        PovComponent.m_HorizontalAxis.Value = Mathf.LerpAngle(CurrentYaw, TargetYaw, T);

        float CurrentPitch = PovComponent.m_VerticalAxis.Value;
        PovComponent.m_VerticalAxis.Value = Mathf.Lerp(CurrentPitch, TargetPitch, T);
    }

    /* 切换锁定：已锁定则解锁，未锁定则尝试锁定最合适的敌人 */
    public void ToggleLock()
    {
        if (bIsLocked)
        {
            Unlock();
            return;
        }

        Enemy Target = FindBestTarget();

        if (Target == null)
        {
            Debug.Log("锁定失败：搜索范围内没有可锁定的敌人");
            return;
        }

        Lock(Target);
    }

    private void Lock(Enemy Target)
    {
        if (Target == null) return;

        CachePovInput();
        CachePivot();

        LockedEnemy = Target;

        /* 切断鼠标对 POV 的控制，否则玩家一动鼠标就把锁定方向拽走 */
        PovComponent.m_HorizontalAxis.m_InputAxisName = string.Empty;
        PovComponent.m_VerticalAxis.m_InputAxisName = string.Empty;
        PovComponent.m_HorizontalAxis.m_InputAxisValue = 0f;
        PovComponent.m_VerticalAxis.m_InputAxisValue = 0f;

        /* 抬高观察支点：相机由「绕脚踝转」变成「绕胸口转」，
           否则相机位置太低，看向同身高的敌人只能俯视 */
        if (FramingTransposer != null)
        {
            FramingTransposer.m_TrackedObjectOffset = DefaultTrackedObjectOffset + Vector3.up * PivotLift;
        }

        Debug.Log($"已锁定敌人：{Target.name}");
    }

    /* 解除锁定并恢复鼠标控制与观察支点。可重复调用 */
    public void Unlock()
    {
        if (LockedEnemy == null) return;

        LockedEnemy = null;

        RestorePovInput();
        RestorePivot();
    }

    /* 还原 POV 的输入轴，让鼠标视角重新生效 */
    private void RestorePovInput()
    {
        if (PovComponent == null) return;
        if (!bHasCachedPovInput) return;

        PovComponent.m_HorizontalAxis.m_InputAxisName = DefaultHorizontalAxisName;
        PovComponent.m_VerticalAxis.m_InputAxisName = DefaultVerticalAxisName;
        PovComponent.m_HorizontalAxis.m_InputAxisValue = 0f;
        PovComponent.m_VerticalAxis.m_InputAxisValue = 0f;
    }

    /* 还原观察支点偏移，让相机回到原来的跟随高度 */
    private void RestorePivot()
    {
        if (FramingTransposer == null) return;
        if (!bHasCachedPivot) return;

        FramingTransposer.m_TrackedObjectOffset = DefaultTrackedObjectOffset;
    }

    /* 对象被禁用/销毁时恢复，避免下次启用时鼠标视角失效、支点高度不对 */
    private void OnDisable()
    {
        LockedEnemy = null;

        RestorePovInput();
        RestorePivot();
    }

    /* 在搜索锥内挑选最合适的敌人。
       优先取相机正前方夹角小的目标，夹角接近时再取距离近的 */
    private Enemy FindBestTarget()
    {
        Collider[] HitColliders = Physics.OverlapSphere(transform.position, LockRange);

        Enemy BestTarget = null;
        float BestAngle = float.MaxValue;
        float BestDistance = float.MaxValue;

        /* 相机正前方。竖直方向置零，避免相机俯仰时把搜索锥压扁 */
        Vector3 CameraForward = transform.forward;
        CameraForward.y = 0f;
        if (CameraForward.sqrMagnitude < 0.0001f) CameraForward = Vector3.forward;

        foreach (var HitCollider in HitColliders)
        {
            //Collider HitCollider = HitColliders[i];
            
            if (!HitCollider.CompareTag("Enemy")) continue;

            Enemy Candidate = HitCollider.GetComponentInParent<Enemy>();
            if (Candidate == null) continue;
            if (!Candidate.RuntimeCommonData.CheckIsHaveHealth()) continue;

            Vector3 ToCandidate = Candidate.transform.position - transform.position;
            ToCandidate.y = 0f;

            float Distance = ToCandidate.magnitude;
            if (Distance > LockRange) continue;

            /* 与相机正前方的夹角，超过半角说明在搜索锥之外 */
            float Angle = Vector3.Angle(CameraForward, ToCandidate);
            if (Angle > LockSearchAngle / 2f) continue;

            //检测是否阻挡
            if (bCheckObstacle && IsBlocked(ToCandidate.normalized, Distance)) continue;

            bool bBetter = Angle < BestAngle - 0.01f
                           || (Mathf.Abs(Angle - BestAngle) <= 0.01f 
                               && Distance < BestDistance);

            if (!bBetter) continue;

            BestTarget = Candidate;
            BestAngle = Angle;
            BestDistance = Distance;
        }

        return BestTarget;
    }

    /* 相机与敌人之间是否有障碍物。从相机稍高的位置打，避免射线贴地自相交 */
    private bool IsBlocked(Vector3 Direction, float Distance)
    {
        if (ObstacleMask.value == 0) return false;

        RaycastHit Hit;
        return Physics.Raycast(
            transform.position + Vector3.up * 0.5f,
            Direction,
            out Hit,
            Distance,
            ObstacleMask);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = bIsLocked ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, LockRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, BreakLockDistance);

        if (!Application.isPlaying) return;
        if (LockedEnemy == null) return;

        /* 画出实际瞄准的点，方便对照 AimHeightOffset 调参 */
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(
            LockedEnemy.transform.position + Vector3.up * AimHeightOffset, 0.08f);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, LockedEnemy.transform.position);
    }
}
