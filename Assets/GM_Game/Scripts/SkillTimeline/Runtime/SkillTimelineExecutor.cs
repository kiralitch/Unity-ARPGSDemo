using System.Collections.Generic;
using UnityEngine;

/*
 * 技能时间轴执行器
 *
 * 职责（纯数据驱动，不改任何战斗状态机）：
 *   1. 给定「当前连招段 + 本段已播放时间 + 上一帧时间」，找出这一帧跨过的事件时间点并执行
 *   2. 找出这一帧结束持续时间的事件并收尾（关闭命中盒）
 *   3. 每段开始时重置触发标记
 *
 * 不负责的事情：
 *   - 不读取动画图内部时间（时间由调用方从 AnimaClipsData 采样后传入）
 *   - 不切换状态、不影响连招、不影响权重
 *   - 不参与 Update 每帧轮询：只有战斗状态在推进动画时才会被调用
 *
 * 时间语义：
 *   t 为「本段动画从 0 开始的已播放秒数」，事件在 t >= evt.Time 时触发，
 *   t >= evt.Time + evt.Duration 时收尾。
 */

namespace Game.SkillTimeline
{
    public sealed class SkillTimelineExecutor
    {
        private readonly Dictionary<SkillEventType, ISkillTimelineEventHandler> _handlers
            = new Dictionary<SkillEventType, ISkillTimelineEventHandler>();

        private SkillTimelineRuntime _runtime;
        private int _currentClipIndex = -1;
        private float _lastTime;

        /// <summary>最近一次被触发的事件（测试与 GM 调试用）</summary>
        public List<SkillTimelineEvent> FiredEvents { get; } = new List<SkillTimelineEvent>();

        public int CurrentClipIndex => _currentClipIndex;
        public bool IsBound => _runtime != null;

        public SkillTimelineExecutor()
        {
            RegisterDefaultHandlers();
        }

        private void RegisterDefaultHandlers()
        {
            Register(new PlaySoundHandler());
            Register(new SpawnVfxHandler());
            Register(new DamageHandler());
        }

        public void Register(ISkillTimelineEventHandler handler)
        {
            if (handler == null) return;
            _handlers[handler.Type] = handler;
        }

        public ISkillTimelineEventHandler GetHandler(SkillEventType type)
        {
            _handlers.TryGetValue(type, out var handler);
            return handler;
        }

        /// <summary>
        /// 绑定一段技能的时间轴。segmentCount 为该技能连招动画段数。
        /// 每次开始一段攻击时调用一次。
        /// </summary>
        public void Bind(SkillTimeline timeline, int segmentCount)
        {
            _runtime = new SkillTimelineRuntime(timeline, segmentCount);
            _currentClipIndex = -1;
            _lastTime = 0f;
            FiredEvents.Clear();
        }

        public void Unbind()
        {
            _runtime = null;
            _currentClipIndex = -1;
            _lastTime = 0f;
        }

        /// <summary>
        /// 进入某一段动画，重置上一段未收尾的事件，并把时间基准归零。
        /// 若上一段有持续时间未结束的事件（例如命中盒还开着），在这里强制收尾，
        /// 否则连招切段后命中盒会一直开到最后。
        /// </summary>
        public void BeginSegment(int clipIndex, SkillExecutionContext context)
        {
            if (_runtime == null) return;

            if (_currentClipIndex >= 0 && _currentClipIndex != clipIndex)
            {
                ForceCloseSegment(_currentClipIndex, context);
            }

            _currentClipIndex = clipIndex;
            _lastTime = 0f;

            var events = _runtime.GetEvents(clipIndex);
            for (int i = 0; i < events.Length; i++)
            {
                events[i].IsTriggered = false;
            }
        }

        /// <summary>
        /// 推进本段动画时间。
        /// </summary>
        /// <param name="clipTime">本段动画已播放秒数（动画时间，受播放速度影响）</param>
        /// <param name="context">执行上下文</param>
        /// <returns>本帧是否有任何事件被触发或收尾</returns>
        public bool Tick(float clipTime, SkillExecutionContext context)
        {
            if (_runtime == null || _currentClipIndex < 0) return false;

            //动画时间回退说明该段被重新播放，按新的一轮处理
            if (clipTime < _lastTime)
            {
                BeginSegment(_currentClipIndex, context);
            }

            bool anyChanged = false;
            var events = _runtime.GetEvents(_currentClipIndex);

            for (int i = 0; i < events.Length; i++)
            {
                var evt = events[i];
                if (evt == null) continue;

                if (!evt.IsTriggered && clipTime >= evt.Time)
                {
                    evt.IsTriggered = true;
                    anyChanged = true;
                    FiredEvents.Add(evt);
                    ExecuteSafely(evt, context);
                }

                if (evt.IsTriggered && evt.Duration > 0f && clipTime >= evt.EndTime)
                {
                    //用 IsTriggered 以外的标记不可靠，这里靠 Finish 幂等实现
                    FinishSafely(evt, context);
                }
            }

            _lastTime = clipTime;
            return anyChanged;
        }

        /// <summary>攻击结束时调用：对当前段所有未收尾的持续时间事件做收尾</summary>
        public void EndSegment(SkillExecutionContext context)
        {
            if (_runtime == null) return;
            if (_currentClipIndex >= 0) ForceCloseSegment(_currentClipIndex, context);
            _currentClipIndex = -1;
            _lastTime = 0f;
        }

        private void ForceCloseSegment(int clipIndex, SkillExecutionContext context)
        {
            var events = _runtime.GetEvents(clipIndex);
            for (int i = 0; i < events.Length; i++)
            {
                var evt = events[i];
                if (evt == null || !evt.IsTriggered) continue;
                if (evt.Duration <= 0f) continue;
                FinishSafely(evt, context);
            }
        }

        private void ExecuteSafely(SkillTimelineEvent evt, SkillExecutionContext context)
        {
            if (!_handlers.TryGetValue(evt.Type, out var handler)) return;

            try
            {
                handler.Execute(evt, context);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SkillTimeline] 执行事件 {evt.Type} 失败：{e}");
            }
        }

        private void FinishSafely(SkillTimelineEvent evt, SkillExecutionContext context)
        {
            if (!_handlers.TryGetValue(evt.Type, out var handler)) return;

            try
            {
                handler.Finish(evt, context);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SkillTimeline] 收尾事件 {evt.Type} 失败：{e}");
            }
        }
    }
}
