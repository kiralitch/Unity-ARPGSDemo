using System;
using System.Collections.Generic;
using UnityEngine;

/*
 * 技能时间轴数据
 *
 * 一个技能 = 若干段连招动画 + 每段动画上的时间轴事件。
 * 之所以按「段落」存储：AbilitiesData.AttackClips 是连招列表，
 * 每段动画各自从 0 开始计时，事件必须落在具体的某一段上。
 *
 * 与原有数据结构的关系：
 *   - AbilitiesData.TimelineSegments 是本类的列表（新增字段，可为 null）
 *   - 原有 AbilitiesData.Events（SkillAnimationEvent）保持不动
 *   - 原有动画关键帧事件（CommonAnimationEventTrigger）保持不动
 */

namespace Game.SkillTimeline
{
    [Serializable]
    public class SkillTimelineSegment
    {
        [Tooltip("连招段索引，对应 AbilitiesData.AttackClips 的下标")]
        public int ClipIndex;

        [Tooltip("该段动画的事件列表")]
        public List<SkillTimelineEvent> Events = new List<SkillTimelineEvent>();

        public SkillTimelineSegment()
        {
        }

        public SkillTimelineSegment(int clipIndex)
        {
            ClipIndex = clipIndex;
        }

        public SkillTimelineSegment Clone()
        {
            var segment = new SkillTimelineSegment(ClipIndex);
            if (Events != null)
            {
                foreach (var e in Events)
                {
                    if (e != null) segment.Events.Add(e.Clone());
                }
            }
            return segment;
        }
    }

    [Serializable]
    public class SkillTimeline
    {
        public List<SkillTimelineSegment> Segments = new List<SkillTimelineSegment>();

        /// <summary>取某段的轨道数据，不存在则返回 null（不自动创建，避免运行时偷偷修改资产）</summary>
        public SkillTimelineSegment FindSegment(int clipIndex)
        {
            if (Segments == null) return null;
            for (int i = 0; i < Segments.Count; i++)
            {
                var s = Segments[i];
                if (s != null && s.ClipIndex == clipIndex) return s;
            }
            return null;
        }

        /// <summary>取某段的事件列表，没有返回空列表</summary>
        public IReadOnlyList<SkillTimelineEvent> GetEvents(int clipIndex)
        {
            var segment = FindSegment(clipIndex);
            if (segment?.Events != null) return segment.Events;
            return EmptyEvents;
        }

        /// <summary>把连续的重复 ClipIndex 合并，用于导入/校验</summary>
        public void RemoveDuplicateSegments()
        {
            if (Segments == null) return;

            var seen = new HashSet<int>();
            for (int i = Segments.Count - 1; i >= 0; i--)
            {
                var s = Segments[i];
                if (s == null)
                {
                    Segments.RemoveAt(i);
                    continue;
                }
                if (!seen.Add(s.ClipIndex)) Segments.RemoveAt(i);
            }
        }

        public List<SkillTimelineEvent> GetAllEventsSorted()
        {
            var result = new List<SkillTimelineEvent>();
            if (Segments == null) return result;

            foreach (var s in Segments)
            {
                if (s?.Events == null) continue;
                foreach (var e in s.Events)
                {
                    if (e != null) result.Add(e);
                }
            }
            return result;
        }

        private static readonly SkillTimelineEvent[] EmptyEvents = Array.Empty<SkillTimelineEvent>();
    }

    /// <summary>运行时按段缓存的事件表，避免每帧 LINQ/查找</summary>
    public sealed class SkillTimelineRuntime
    {
        private readonly SkillTimelineEvent[][] _events;

        public SkillTimelineRuntime(SkillTimeline timeline, int segmentCount)
        {
            _events = new SkillTimelineEvent[Mathf.Max(1, segmentCount)][];
            for (int i = 0; i < _events.Length; i++)
            {
                var source = timeline?.GetEvents(i);
                _events[i] = source != null && source.Count > 0 ? CopyArray(source) : Array.Empty<SkillTimelineEvent>();
            }
        }

        public SkillTimelineEvent[] GetEvents(int segmentIndex)
        {
            if (segmentIndex < 0 || segmentIndex >= _events.Length) return Array.Empty<SkillTimelineEvent>();
            return _events[segmentIndex];
        }

        public void ResetAllTriggers()
        {
            foreach (var list in _events)
            {
                foreach (var e in list) e.IsTriggered = false;
            }
        }

        private static SkillTimelineEvent[] CopyArray(IReadOnlyList<SkillTimelineEvent> source)
        {
            var arr = new SkillTimelineEvent[source.Count];
            for (int i = 0; i < source.Count; i++) arr[i] = source[i];
            return arr;
        }
    }
}
