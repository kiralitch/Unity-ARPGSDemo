using UnityEngine;

/*
 * 技能时间轴执行器宿主
 *
 * 每个角色（Player / Enemy）一个，负责：
 *   - 持有 SkillTimelineExecutor
 *   - 每帧把「本段动画已播放时间」喂给执行器
 *
 * 为什么需要它：AnimaClipsData 内部持有的 AnimationClipPlayable 是私有的，
 * 外部拿不到，因此这里只读采样动画实际播放时间（GetCurrentAttackClipTime），
 * 保证时间轴与动画严格同步（受动画速度、丢帧、重启影响）。
 * 采样失败时才退回到「累加 deltaTime × 速度」的估算值。
 *
 * 接入方式（每个角色一处，只需一行）：
 *   Player_AttackState.PhysicsUpdate() -> PlayerRef.SkillTimeline.OnAttackFrame(...)
 *   Enemy_AttackState.PhysicsUpdate()  -> CachedEnemy.SkillTimeline.OnAttackFrame(...)
 *
 * 编辑器预览：IsPreview = true 时由编辑器调用 DrivePreview 推进，不产生真实伤害/特效。
 */

namespace Game.SkillTimeline
{
    public class SkillTimelineTicker : MonoBehaviour
    {
        private readonly SkillTimelineExecutor _executor = new SkillTimelineExecutor();

        /// <summary>时间轴执行器（GM/调试/测试读取）</summary>
        public SkillTimelineExecutor Executor => _executor;

        private CommonActor _owner;
        private Transform _attachRoot;
        private AnimaClipsData _clipsData;

        private float _estimatedTime;
        private float _segmentSpeed = 1f;
        private int _segmentIndex = -1;
        private float _baseDamage;

        //每段连招动画时长（秒），用于把事件时间钳制在本段内
        private float[] _clipLengths;
        private float _segmentClipLength;

        /// <summary>编辑器预览用：预览时禁止实际伤害/特效残留</summary>
        public bool IsPreview { get; set; }

        /// <summary>预览时由编辑器提供动画时间，返回负数表示不接管</summary>
        public System.Func<float> PreviewTimeProvider { get; set; }

        public int CurrentSegment => _segmentIndex;
        public float CurrentSegmentTime => _estimatedTime;

        /// <summary>手动指定角色与动画数据（不调用则 Awake 时自动解析）</summary>
        public void Init(CommonActor owner, Transform attachRoot, AnimaClipsData clipsData)
        {
            _owner = owner;
            _attachRoot = attachRoot;
            _clipsData = clipsData;
        }

        private void Awake()
        {
            //未手动 Init 时自动解析，减少场景接线步骤
            if (_owner == null)
            {
                _owner = GetComponent<CommonActor>() ?? GetComponentInParent<CommonActor>();
            }
            AutoResolveRefs();
        }

        /// <summary>补齐 owner 对应的参考引用（运行时父级可能尚未启用）</summary>
        private void AutoResolveRefs()
        {
            if (_attachRoot == null && _owner != null)
            {
                _attachRoot = _owner.transform;
            }
            if (_clipsData == null && _owner != null)
            {
                if (_owner is Player player) _clipsData = player.AnimationClipData;
                else if (_owner is Enemy enemy) _clipsData = enemy.AnimationClipData;
            }
        }

        /// <summary>
        /// 开始一段攻击：绑定时间轴、进入段 0。
        /// 由攻击状态在播放第一段动画后调用。
        /// </summary>
        /// <param name="startSegment">本次实际播放的连招段索引（玩家连续按攻击时会依次推进 0/1/2…）</param>
        /// <param name="clipLengths">每段动画时长（秒，可选）。用于把事件时间钳制在本段内，不传则不钳制</param>
        public void BeginAttack(SkillTimeline timeline, int segmentCount, float baseDamage, float animationSpeed,
            int startSegment = 0, float[] clipLengths = null)
        {
            _baseDamage = baseDamage;
            _segmentSpeed = Mathf.Max(0.01f, animationSpeed);
            _segmentIndex = -1;
            _estimatedTime = 0f;
            _segmentClipLength = 0f;
            _clipLengths = clipLengths;

            //敌人连招切段通知（Player 的连招没有切段回调，这里订阅也不会被触发）
            if (_clipsData != null)
            {
                _clipsData.OnEnemyComboSegmentChanged -= HandleComboSegmentChanged;
                _clipsData.OnEnemyComboSegmentChanged += HandleComboSegmentChanged;
            }

            _executor.Bind(timeline, segmentCount);
            //进入本次真正播放的段，而不是固定第 0 段
            EnterSegment(startSegment);
        }

        /// <summary>连招切到下一段时调用（只重置时间基准，不重新绑定）</summary>
        /// <param name="clipLength">该段动画时长（秒），<=0 表示不钳制事件时间</param>
        public void EnterSegment(int segmentIndex, float clipLength = 0f)
        {
            if (segmentIndex < 0) segmentIndex = 0;
            _segmentIndex = segmentIndex;
            _estimatedTime = 0f;
            _segmentClipLength = clipLength > 0f ? clipLength : GetClipLength(segmentIndex);
            _executor.BeginSegment(segmentIndex, CreateContext());
        }

        /// <summary>
        /// 再给执行器一次机会推进到本段末尾：
        /// 本段时长比上一段短时，事件时间会被钳制到本段末尾，
        /// 而状态切换可能发生在时间越过末尾之后，这里保证到点事件不会漏触发。
        /// </summary>
        private void FlushSegmentEnd()
        {
            if (_segmentIndex < 0 || _segmentClipLength <= 0f) return;
            _estimatedTime = _segmentClipLength;
            _executor.Tick(_segmentClipLength, CreateContext());
        }

        /// <summary>
        /// 攻击状态每帧（PhysicsUpdate）调用一次，取得本段动画时间并执行到点事件。
        /// </summary>
        public bool OnAttackFrame(float deltaTime)
        {
            if (_segmentIndex < 0 || deltaTime <= 0f) return false;

            AutoResolveRefs();

            float clipTime = SampleClipTime(deltaTime);
            return _executor.Tick(clipTime, CreateContext());
        }

        /// <summary>编辑器预览专用：直接推进到指定动画时间（不做增量）</summary>
        public bool DrivePreviewTo(float clipTime)
        {
            if (_segmentIndex < 0) return false;
            _estimatedTime = clipTime;
            return _executor.Tick(clipTime, CreateContext());
        }

        /// <summary>攻击结束：强制收尾持续时间事件（关闭命中盒）</summary>
        public void EndAttack()
        {
            if (_clipsData != null)
            {
                _clipsData.OnEnemyComboSegmentChanged -= HandleComboSegmentChanged;
            }

            //段切换可能早于本段动画播完：先补到段末尾，保证钳制在末尾的事件也能触发
            FlushSegmentEnd();

            _executor.EndSegment(CreateContext());
            _executor.Unbind();
            _segmentIndex = -1;
            _estimatedTime = 0f;
        }

        private void OnDisable()
        {
            if (_segmentIndex >= 0) EndAttack();
        }

        private void HandleComboSegmentChanged(int segmentIndex)
        {
            if (_segmentIndex < 0) return;
            //切段前先把上一段钳制在末尾的事件补触发，否则段被切走后就再也触发不到
            FlushSegmentEnd();
            //切段通知在 AnimaClipsData 已经连上新片段之后发出，此时读到的就是新一段的时长
            EnterSegment(segmentIndex, ReadCurrentClipLength());
        }

        /// <summary>取当前正在播放的那一段动画时长；取不到返回 0（表示不钳制）</summary>
        private float ReadCurrentClipLength()
        {
            if (_clipsData != null && _clipsData.CurrentAttackClipLength > 0f)
            {
                return _clipsData.CurrentAttackClipLength;
            }
            return GetClipLength(_segmentIndex);
        }

        private float GetClipLength(int segmentIndex)
        {
            if (_clipLengths == null || segmentIndex < 0 || segmentIndex >= _clipLengths.Length) return 0f;
            return _clipLengths[segmentIndex];
        }

        /// <summary>取本段动画时间；取不到时用 deltaTime 估算。返回值 >= 0</summary>
        private float SampleClipTime(float deltaTime)
        {
            if (IsPreview && PreviewTimeProvider != null)
            {
                float preview = PreviewTimeProvider();
                if (preview >= 0f) _estimatedTime = preview;
                return ClampToSegment(_estimatedTime);
            }

            if (_clipsData != null)
            {
                float t = _clipsData.GetCurrentAttackClipTime();
                if (t > 0f)
                {
                    _estimatedTime = t;
                    return ClampToSegment(t);
                }
            }

            _estimatedTime += deltaTime * _segmentSpeed;
            return ClampToSegment(_estimatedTime);
        }

        /* 事件时间钳制在本段动画时长内：
           每段的动画时长可能不同，事件时间超出本段时按本段末尾处理，
           否则三段时长不等时，事件会在本段还没放完时就提前触发 */
        private float ClampToSegment(float clipTime)
        {
            if (_segmentClipLength > 0f && clipTime > _segmentClipLength)
            {
                _estimatedTime = _segmentClipLength;
                return _segmentClipLength;
            }
            return clipTime;
        }

        private SkillExecutionContext CreateContext()
        {
            return new SkillExecutionContext(
                _owner,
                _attachRoot,
                Mathf.Max(0, _segmentIndex),
                _baseDamage,
                IsPreview);
        }
    }
}
