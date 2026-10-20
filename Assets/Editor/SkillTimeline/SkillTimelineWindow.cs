using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/*
 * 技能时间轴编辑器（自研时间轴窗口）
 *
 * 用途：
 *   - 选择 Player_AbilitySet / Enemy_AbilitySet，定位到某个技能
 *   - 每段连招动画一条轨道，点击/拖拽放置与移动事件
 *   - 编辑事件参数：时刻、持续、资源地址、挂点、倍率
 *   - 用 AnimationMode.SampleAnimationClip 预览任意时刻的角色姿势
 *
 * 约束：
 *   - 只写入 AbilitiesData.Timeline / EnemyAbilityData.Timeline
 *   - 不改动 AttackClips、Events、状态机
 *   - 预览不创建 PlayableDirector，不与运行时 PlayableGraph 抢 Animator
 */

namespace Game.SkillTimeline.EditorTools
{
    public class SkillTimelineWindow : EditorWindow
    {
        private const float TrackHeaderWidth = 190f;
        private const float RulerHeight = 22f;
        private const float LaneHeight = 30f;
        private const float MarkerWidth = 12f;
        private const float MinPixelsPerSecond = 20f;
        private const float MaxPixelsPerSecond = 600f;
        private const string PrefLastSet = "SkillTimeline.LastSet";

        [SerializeField] private Player_AbilitySet _playerSet;
        [SerializeField] private Enemy_AbilitySet _enemySet;
        [SerializeField] private bool _editingEnemy;
        [SerializeField] private int _skillIndex;
        [SerializeField] private int _selectedSegment;
        [SerializeField] private int _selectedEvent = -1;

        private float _pixelsPerSecond = 160f;
        private Vector2 _scroll;
        private float _previewTime;
        private bool _previewing;
        private double _lastEditorTime;
        private GameObject _previewTarget;
        private GUIStyle _labelStyle;

        private int _dragSegment = -1;
        private int _dragEvent = -1;

        [MenuItem("Tools/Skill Timeline Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<SkillTimelineWindow>("技能时间轴");
            window.minSize = new Vector2(760f, 460f);
            window.RestoreLastSet();
        }

        private void OnEnable()
        {
            RestoreLastSet();
            EditorApplication.update += OnEditorUpdate;
            if (!AnimationMode.InAnimationMode()) AnimationMode.StartAnimationMode();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            _previewing = false;
        }

        private void RestoreLastSet()
        {
            if (_playerSet != null || _enemySet != null) return;

            string path = EditorPrefs.GetString(PrefLastSet, string.Empty);
            if (string.IsNullOrEmpty(path)) return;

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset is Player_AbilitySet player) { _playerSet = player; _editingEnemy = false; }
            else if (asset is Enemy_AbilitySet enemy) { _enemySet = enemy; _editingEnemy = true; }
        }

        #region 数据访问

        private UnityEngine.Object CurrentSet => _editingEnemy ? (UnityEngine.Object)_enemySet : _playerSet;

        private bool HasSet => CurrentSet != null;

        private List<AnimationClip> GetClips(int skillIndex)
        {
            if (_editingEnemy)
            {
                var list = _enemySet?.AbilitiesList;
                return list != null && skillIndex >= 0 && skillIndex < list.Count ? list[skillIndex]?.AttackClips : null;
            }

            var playerList = _playerSet?.AbilitiesList;
            return playerList != null && skillIndex >= 0 && skillIndex < playerList.Count ? playerList[skillIndex]?.AttackClips : null;
        }

        private string GetSkillName(int index)
        {
            if (_editingEnemy)
            {
                var list = _enemySet?.AbilitiesList;
                return list != null && index >= 0 && index < list.Count ? list[index]?.skillName : null;
            }

            var playerList = _playerSet?.AbilitiesList;
            return playerList != null && index >= 0 && index < playerList.Count ? playerList[index]?.skillName : null;
        }

        private float GetSpeed(int skillIndex)
        {
            float speed = _editingEnemy
                ? _enemySet?.AbilitiesList?[skillIndex]?.animationSpeed ?? 1f
                : _playerSet?.AbilitiesList?[skillIndex]?.animationSpeed ?? 1f;
            return Mathf.Max(0.01f, speed);
        }

        private SkillTimeline GetTimeline(int skillIndex)
        {
            return _editingEnemy
                ? _enemySet?.AbilitiesList?[skillIndex]?.Timeline
                : _playerSet?.AbilitiesList?[skillIndex]?.Timeline;
        }

        private int SkillCount => _editingEnemy
            ? _enemySet?.AbilitiesList?.Count ?? 0
            : _playerSet?.AbilitiesList?.Count ?? 0;

        private float GetDuration(int segment)
        {
            var clips = GetClips(_skillIndex);
            if (clips == null || segment < 0 || segment >= clips.Count || clips[segment] == null) return 1f;
            return clips[segment].length;
        }

        private SerializedProperty GetSkillProperty(SerializedObject so, int index)
        {
            var list = so?.FindProperty("abilitiesList");
            if (list == null || index < 0 || index >= list.arraySize) return null;
            return list.GetArrayElementAtIndex(index);
        }

        /* 取技能的 Timeline 属性。
           兼容两种声明方式：普通字段 "timeline"，以及自动属性的背后字段 "<Timeline>k__BackingField"。
           后者在 FindPropertyRelative("Timeline") 下是找不到的，这里统一兜底，避免再次出现“找不到 Timeline.Segments”。 */
        private static SerializedProperty GetTimelineProperty(SerializedProperty skillProp)
        {
            if (skillProp == null) return null;

            var timeline = skillProp.FindPropertyRelative("timeline");
            if (timeline == null) timeline = skillProp.FindPropertyRelative("Timeline");
            if (timeline == null) timeline = skillProp.FindPropertyRelative("<Timeline>k__BackingField");
            return timeline;
        }

        private SerializedProperty GetSegmentsProperty(SerializedObject so)
        {
            var timeline = GetTimelineProperty(GetSkillProperty(so, _skillIndex));
            return timeline?.FindPropertyRelative("Segments");
        }

        #endregion

        #region 主窗口

        private void OnGUI()
        {
            DrawToolbar();
            if (!HasSet)
            {
                EditorGUILayout.HelpBox("请选择 Player_AbilitySet 或 Enemy_AbilitySet。", MessageType.Info);
                return;
            }
            if (SkillCount == 0)
            {
                EditorGUILayout.HelpBox("该技能集中没有技能。", MessageType.Warning);
                return;
            }

            _skillIndex = Mathf.Clamp(_skillIndex, 0, SkillCount - 1);
            DrawSkillSelector();

            if (Event.current != null && Event.current.type == EventType.Layout)
            {
                _selectedSegment = Mathf.Clamp(_selectedSegment, 0, Mathf.Max(0, (GetClips(_skillIndex)?.Count ?? 1) - 1));
            }

            DrawTrackArea();
            DrawFooter();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            _editingEnemy = GUILayout.Toolbar(_editingEnemy ? 1 : 0, new[] { "玩家技能", "敌人技能" }, EditorStyles.toolbarButton, GUILayout.Width(150)) == 1;
            GUILayout.Space(8);

            EditorGUI.BeginChangeCheck();
            if (_editingEnemy)
            {
                _enemySet = (Enemy_AbilitySet)EditorGUILayout.ObjectField(_enemySet, typeof(Enemy_AbilitySet), false, GUILayout.Width(250));
            }
            else
            {
                _playerSet = (Player_AbilitySet)EditorGUILayout.ObjectField(_playerSet, typeof(Player_AbilitySet), false, GUILayout.Width(250));
            }
            if (EditorGUI.EndChangeCheck()) OnSetChanged();

            using (new EditorGUI.DisabledScope(!HasSet))
            {
                if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(45)))
                {
                    EditorGUIUtility.PingObject(CurrentSet);
                }
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField("缩放", GUILayout.Width(30));
            _pixelsPerSecond = EditorGUILayout.Slider(_pixelsPerSecond, MinPixelsPerSecond, MaxPixelsPerSecond, GUILayout.Width(160));

            EditorGUILayout.EndHorizontal();
        }

        private void OnSetChanged()
        {
            _selectedEvent = -1;
            _selectedSegment = 0;
            _previewTime = 0f;
            _previewing = false;

            if (CurrentSet != null) EditorPrefs.SetString(PrefLastSet, AssetDatabase.GetAssetPath(CurrentSet));
        }

        private void DrawSkillSelector()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("技能", GUILayout.Width(30));
            var names = new List<string>();
            for (int i = 0; i < SkillCount; i++)
            {
                string name = GetSkillName(i);
                names.Add(string.IsNullOrEmpty(name) ? $"技能 {i}" : $"{i}. {name}");
            }
            _skillIndex = EditorGUILayout.Popup(_skillIndex, names.ToArray(), GUILayout.Width(240));

            int clipCount = GetClips(_skillIndex)?.Count ?? 0;
            EditorGUILayout.LabelField($"连段数：{clipCount}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"动画速度 x{GetSpeed(_skillIndex):0.##}", GUILayout.Width(110));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("初始化时间轴", GUILayout.Width(100))) EnsureTimeline();
            if (GUILayout.Button("添加事件", GUILayout.Width(80))) AddEventAtPlayhead();
            using (new EditorGUI.DisabledScope(_selectedEvent < 0))
            {
                if (GUILayout.Button("删除事件", GUILayout.Width(80))) RemoveSelectedEvent();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTrackArea()
        {
            var clips = GetClips(_skillIndex);
            if (clips == null || clips.Count == 0)
            {
                EditorGUILayout.HelpBox("该技能没有配置攻击动画（AttackClips）。", MessageType.Warning);
                return;
            }

            var timeline = GetTimeline(_skillIndex);
            if (timeline == null)
            {
                EditorGUILayout.HelpBox("该技能还没有时间轴数据，请点击「初始化时间轴」。", MessageType.Info);
                return;
            }

            float contentWidth = Mathf.Max(position.width - TrackHeaderWidth, 1f);
            float contentHeight = RulerHeight + clips.Count * LaneHeight;

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));

            Rect contentRect = GUILayoutUtility.GetRect(contentWidth, contentHeight);
            Rect fullRect = new Rect(contentRect.x, contentRect.y, contentWidth, contentHeight);

            DrawRuler(fullRect, clips);
            DrawLanes(fullRect, clips, timeline);
            HandleInput(fullRect, clips);

            EditorGUILayout.EndScrollView();
        }

        private Rect GetLaneRect(Rect fullRect, int segment)
        {
            return new Rect(fullRect.x + TrackHeaderWidth, fullRect.y + RulerHeight + segment * LaneHeight,
                fullRect.width - TrackHeaderWidth, LaneHeight);
        }

        private void DrawRuler(Rect fullRect, List<AnimationClip> clips)
        {
            Rect ruler = new Rect(fullRect.x + TrackHeaderWidth, fullRect.y, fullRect.width - TrackHeaderWidth, RulerHeight);
            EditorGUI.DrawRect(ruler, new Color(0.16f, 0.16f, 0.16f));
            EditorGUI.DrawRect(new Rect(fullRect.x, fullRect.y, TrackHeaderWidth, fullRect.height), new Color(0.19f, 0.19f, 0.19f));

            float duration = GetDuration(_selectedSegment);
            float step = ChooseTickStep(duration);

            for (float t = 0f; t <= duration + 0.0001f; t += step)
            {
                float x = TimeToX(fullRect, t);
                EditorGUI.DrawRect(new Rect(x, ruler.y, 1f, ruler.height), new Color(1f, 1f, 1f, 0.2f));
                GUI.Label(new Rect(x + 2f, ruler.y + 3f, 80f, 16f), t.ToString("0.##") + "s", GetLabelStyle());
            }

            // 播放头
            float previewX = TimeToX(fullRect, Mathf.Clamp(_previewTime, 0f, duration));
            EditorGUI.DrawRect(new Rect(previewX - 0.5f, fullRect.y, 1f, fullRect.height), new Color(1f, 0.45f, 0.1f));
        }

        private void DrawLanes(Rect fullRect, List<AnimationClip> clips, SkillTimeline timeline)
        {
            for (int seg = 0; seg < clips.Count; seg++)
            {
                Rect lane = GetLaneRect(fullRect, seg);
                float duration = GetDuration(seg);

                EditorGUI.DrawRect(lane, seg == _selectedSegment
                    ? new Color(0.27f, 0.33f, 0.42f)
                    : new Color(0.24f, 0.24f, 0.24f));
                EditorGUI.DrawRect(new Rect(lane.x, lane.yMax - 1f, lane.width, 1f), new Color(0f, 0f, 0f, 0.45f));

                var clip = clips[seg];
                GUI.Label(new Rect(fullRect.x + 6f, lane.y + 6f, TrackHeaderWidth - 12f, 18f),
                    $"{seg}: {(clip != null ? clip.name : "(空)")}", GetLabelStyle());

                var segment = timeline.FindSegment(seg);
                if (segment?.Events == null) continue;

                for (int i = 0; i < segment.Events.Count; i++)
                {
                    var evt = segment.Events[i];
                    if (evt == null) continue;

                    float x = TimeToX(fullRect, Mathf.Clamp(evt.Time, 0f, duration));
                    Color color = GetEventColor(evt.Type);

                    if (evt.Duration > 0f)
                    {
                        float endX = TimeToX(fullRect, Mathf.Clamp(evt.EndTime, 0f, duration));
                        EditorGUI.DrawRect(new Rect(x + MarkerWidth * 0.5f, lane.y + 10f, Mathf.Max(0f, endX - x), LaneHeight - 22f),
                            new Color(color.r, color.g, color.b, 0.35f));
                    }

                    Rect marker = new Rect(x - MarkerWidth * 0.5f, lane.y + 5f, MarkerWidth, LaneHeight - 12f);
                    bool selected = seg == _selectedSegment && i == _selectedEvent;
                    EditorGUI.DrawRect(marker, selected ? Color.white : color);

                    GUI.Label(new Rect(x + 9f, lane.y + 6f, 170f, 18f), evt.GetTimelineLabel(), GetLabelStyle());
                }
            }
        }

        private void HandleInput(Rect fullRect, List<AnimationClip> clips)
        {
            var e = Event.current;
            if (e == null) return;

            Rect timelineArea = new Rect(fullRect.x + TrackHeaderWidth, fullRect.y, fullRect.width - TrackHeaderWidth, fullRect.height);
            if (!timelineArea.Contains(e.mousePosition) && e.type != EventType.MouseUp && e.type != EventType.MouseDrag) return;

            switch (e.type)
            {
                case EventType.MouseDown when e.button == 0 && timelineArea.Contains(e.mousePosition):
                    if (TryPickEvent(fullRect, clips, e.mousePosition, out int seg, out int index))
                    {
                        _selectedSegment = seg;
                        _selectedEvent = index;
                        _dragSegment = seg;
                        _dragEvent = index;
                        e.Use();
                        break;
                    }

                    if (e.mousePosition.y > fullRect.y + RulerHeight)
                    {
                        _selectedSegment = Mathf.Clamp(Mathf.FloorToInt((e.mousePosition.y - fullRect.y - RulerHeight) / LaneHeight), 0, clips.Count - 1);
                        _selectedEvent = -1;
                    }

                    _previewTime = Mathf.Clamp(XToTime(fullRect, e.mousePosition.x), 0f, GetDuration(_selectedSegment));
                    SamplePreview(_previewTime);
                    Repaint();
                    e.Use();
                    break;

                case EventType.MouseDrag when _dragEvent >= 0:
                    DragEvent(fullRect, e.mousePosition);
                    e.Use();
                    break;

                case EventType.MouseUp when _dragEvent >= 0:
                    _dragEvent = -1;
                    _dragSegment = -1;
                    e.Use();
                    break;
            }
        }

        private void DragEvent(Rect fullRect, Vector2 mouse)
        {
            var timeline = GetTimeline(_skillIndex);
            var segment = timeline?.FindSegment(_dragSegment);
            if (segment?.Events == null || _dragEvent >= segment.Events.Count) return;

            var evt = segment.Events[_dragEvent];
            float duration = GetDuration(_dragSegment);
            float t = SnapTime(Mathf.Clamp(XToTime(fullRect, mouse.x), 0f, duration));

            // 有持续时间的判定区间不能越过本段结尾
            if (evt.Duration > 0f) t = Mathf.Min(t, Mathf.Max(0f, duration - evt.Duration));
            if (Mathf.Approximately(evt.Time, t)) return;

            Undo.RecordObject(CurrentSet, "移动时间轴事件");
            evt.Time = t;
            EditorUtility.SetDirty(CurrentSet);
            Repaint();
        }

        private bool TryPickEvent(Rect fullRect, List<AnimationClip> clips, Vector2 mouse, out int segment, out int index)
        {
            segment = -1;
            index = -1;
            if (mouse.y < fullRect.y + RulerHeight) return false;

            int seg = Mathf.FloorToInt((mouse.y - fullRect.y - RulerHeight) / LaneHeight);
            if (seg < 0 || seg >= clips.Count) return false;

            var data = GetTimeline(_skillIndex)?.FindSegment(seg);
            if (data?.Events == null) return false;

            float duration = GetDuration(seg);
            for (int i = 0; i < data.Events.Count; i++)
            {
                var evt = data.Events[i];
                if (evt == null) continue;

                float x = TimeToX(fullRect, Mathf.Clamp(evt.Time, 0f, duration));
                if (Mathf.Abs(mouse.x - x) > MarkerWidth * 0.75f) continue;

                segment = seg;
                index = i;
                return true;
            }

            return false;
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal();

            float duration = GetDuration(_selectedSegment);

            if (GUILayout.Button(_previewing ? "停止预览" : "播放预览", GUILayout.Width(80)))
            {
                _previewing = !_previewing;
                _lastEditorTime = EditorApplication.timeSinceStartup;
                if (_previewing && _previewTarget == null) _previewTarget = FindPreviewAnimator();
            }

            if (GUILayout.Button("回到起点", GUILayout.Width(80)))
            {
                _previewTime = 0f;
                SamplePreview(_previewTime);
            }

            EditorGUILayout.LabelField("播放头", GUILayout.Width(48));
            float newTime = EditorGUILayout.Slider(_previewTime, 0f, duration);
            if (!Mathf.Approximately(newTime, _previewTime))
            {
                _previewTime = newTime;
                SamplePreview(_previewTime);
            }

            EditorGUILayout.EndHorizontal();

            DrawEventInspector();
        }

        private void DrawEventInspector()
        {
            var segment = GetTimeline(_skillIndex)?.FindSegment(_selectedSegment);
            if (segment?.Events == null || _selectedEvent < 0 || _selectedEvent >= segment.Events.Count) return;

            var evt = segment.Events[_selectedEvent];
            if (evt == null) return;

            float duration = GetDuration(_selectedSegment);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            evt.Type = (SkillEventType)EditorGUILayout.EnumPopup("类型", evt.Type, GUILayout.Width(230));
            if (!evt.Type.IsImplemented()) EditorGUILayout.LabelField("（预留类型，运行时暂不执行）", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            evt.Time = EditorGUILayout.Slider("时刻(s)", evt.Time, 0f, duration);

            switch (evt.Type)
            {
                case SkillEventType.Damage:
                    evt.Duration = EditorGUILayout.Slider("持续(s)", evt.Duration, 0f, Mathf.Max(0f, duration - evt.Time));
                    evt.DamageScale = EditorGUILayout.FloatField("伤害倍率", evt.DamageScale);
                    break;

                case SkillEventType.PlaySound:
                    DrawAssetPath(ref evt.AssetPath);
                    evt.Volume = EditorGUILayout.Slider("音量", evt.Volume, 0f, 1f);
                    break;

                case SkillEventType.SpawnVfx:
                    DrawAssetPath(ref evt.AssetPath);
                    evt.AttachPoint = EditorGUILayout.TextField("挂点骨骼(空=施法者)", evt.AttachPoint);
                    break;
            }

            EditorGUILayout.EndVertical();

            //只在真正修改时标记资产，避免每帧 Repaint 都把资产标脏
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(CurrentSet, "修改时间轴事件");
                EditorUtility.SetDirty(CurrentSet);
                Repaint();
            }
        }

        private void DrawAssetPath(ref string path)
        {
            EditorGUILayout.BeginHorizontal();
            path = EditorGUILayout.TextField("资源地址", path);
            if (GUILayout.Button("定位", GUILayout.Width(45)))
            {
                var asset = ResolveAssetByPath(path);
                if (asset != null) EditorGUIUtility.PingObject(asset);
                else Debug.LogWarning($"[SkillTimeline] 找不到资源：{path}");
            }
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region 数据操作

        private void EnsureTimeline()
        {
            if (CurrentSet == null) return;

            Undo.RecordObject(CurrentSet, "初始化技能时间轴");

            var so = new SerializedObject(CurrentSet);
            var skillProp = GetSkillProperty(so, _skillIndex);
            if (skillProp == null)
            {
                Debug.LogError($"[SkillTimeline] 找不到 {_skillIndex} 号技能（abilitiesList[{_skillIndex}]），无法初始化时间轴。");
                return;
            }

            int clipCount = GetClips(_skillIndex)?.Count ?? 0;

            var segments = GetTimelineProperty(skillProp)?.FindPropertyRelative("Segments");
            if (segments == null)
            {
                /* Timeline 不是可序列化字段（例如被写成自动属性），SerializedProperty 写不进去。
                   这里无法再可靠地写回资产，只给出明确提示，避免静默失败。 */
                Debug.LogError("[SkillTimeline] Timeline 字段不可序列化，无法初始化时间轴。" +
                               "请把 AbilitiesData/EnemyAbilityData 的 Timeline 声明为 [SerializeField] 普通字段后重新编译。");
                return;
            }

            segments.arraySize = clipCount;
            for (int i = 0; i < clipCount; i++)
            {
                var seg = segments.GetArrayElementAtIndex(i);
                seg.FindPropertyRelative("ClipIndex").intValue = i;
                seg.FindPropertyRelative("Events").arraySize = 0;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(CurrentSet);
            AssetDatabase.SaveAssets();
        }

        private void AddEventAtPlayhead()
        {
            if (CurrentSet == null) return;

            var clips = GetClips(_skillIndex);
            if (clips == null || clips.Count == 0)
            {
                Debug.LogError("[SkillTimeline] 当前技能没有 AttackClips，无法添加时间轴事件。");
                return;
            }

            if (GetTimeline(_skillIndex) == null)
            {
                EnsureTimeline();
                if (GetTimeline(_skillIndex) == null) return;
            }

            int seg = Mathf.Clamp(_selectedSegment, 0, clips.Count - 1);
            float duration = GetDuration(seg);

            Undo.RecordObject(CurrentSet, "添加时间轴事件");

            var so = new SerializedObject(CurrentSet);
            var segments = GetSegmentsProperty(so);
            if (segments == null)
            {
                Debug.LogError("[SkillTimeline] 找不到 Segments 属性，事件未添加。请先确认技能有 AttackClips 并点击「初始化时间轴」。");
                return;
            }

            var segmentProp = FindOrCreateSegment(segments, seg);
            var events = segmentProp.FindPropertyRelative("Events");

            //记录新增元素下标：必须在 ApplyModifiedProperties 之前取。
            //应用后旧 SerializedProperty 的 arraySize / GetArrayElementAtIndex 仍是修改前的快照，
            //在应用后再读 arraySize 会拿到旧值，导致 _selectedEvent 被设成 -1，检查器不显示。
            int newIndex = events.arraySize;

            events.arraySize = newIndex + 1;
            var evt = events.GetArrayElementAtIndex(newIndex);
            evt.FindPropertyRelative("Type").enumValueIndex = (int)SkillEventType.PlaySound;
            evt.FindPropertyRelative("Time").floatValue = Mathf.Clamp(SnapTime(_previewTime), 0f, duration);
            evt.FindPropertyRelative("Duration").floatValue = 0f;
            evt.FindPropertyRelative("Volume").floatValue = 1f;
            evt.FindPropertyRelative("DamageScale").floatValue = 1f;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(CurrentSet);

            _selectedSegment = seg;
            _selectedEvent = newIndex;

            //立即落盘并让编辑器知道资产变了，避免窗口中看不到新事件 / 切换窗口后丢失
            AssetDatabase.SaveAssets();

            Repaint();
        }

        private static SerializedProperty FindOrCreateSegment(SerializedProperty segments, int clipIndex)
        {
            for (int i = 0; i < segments.arraySize; i++)
            {
                var s = segments.GetArrayElementAtIndex(i);
                if (s.FindPropertyRelative("ClipIndex").intValue == clipIndex) return s;
            }

            segments.arraySize++;
            var created = segments.GetArrayElementAtIndex(segments.arraySize - 1);
            created.FindPropertyRelative("ClipIndex").intValue = clipIndex;
            created.FindPropertyRelative("Events").arraySize = 0;
            return created;
        }

        private void RemoveSelectedEvent()
        {
            if (CurrentSet == null || _selectedEvent < 0) return;

            Undo.RecordObject(CurrentSet, "删除时间轴事件");

            var so = new SerializedObject(CurrentSet);
            var segments = GetSegmentsProperty(so);
            if (segments == null)
            {
                Debug.LogError("[SkillTimeline] 找不到 Segments 属性，事件未删除。");
                return;
            }

            for (int i = 0; i < segments.arraySize; i++)
            {
                var seg = segments.GetArrayElementAtIndex(i);
                if (seg.FindPropertyRelative("ClipIndex").intValue != _selectedSegment) continue;

                var events = seg.FindPropertyRelative("Events");
                if (_selectedEvent >= events.arraySize) return;

                //删除后的元素数量要在 ApplyModifiedProperties 之前算好，旧属性快照的 arraySize 不会更新
                int remaining = events.arraySize - 1;

                events.DeleteArrayElementAtIndex(_selectedEvent);
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(CurrentSet);

                _selectedEvent = Mathf.Min(_selectedEvent, remaining - 1);
                Repaint();
                return;
            }
        }

        #endregion

        #region 预览

        private void OnEditorUpdate()
        {
            if (!_previewing) return;

            double now = EditorApplication.timeSinceStartup;
            float delta = (float)(now - _lastEditorTime);
            _lastEditorTime = now;

            float duration = GetDuration(_selectedSegment);
            _previewTime += delta * GetSpeed(_skillIndex);
            if (_previewTime > duration) _previewTime = 0f;

            SamplePreview(_previewTime);
            Repaint();
        }

        private void SamplePreview(float time)
        {
            var clips = GetClips(_skillIndex);
            if (clips == null || _selectedSegment < 0 || _selectedSegment >= clips.Count) return;

            var clip = clips[_selectedSegment];
            if (clip == null) return;

            if (!AnimationMode.InAnimationMode()) AnimationMode.StartAnimationMode();

            if (_previewTarget == null) _previewTarget = FindPreviewAnimator();
            if (_previewTarget == null) return;

            AnimationMode.SampleAnimationClip(_previewTarget, clip, Mathf.Clamp(time, 0f, clip.length));
            SceneView.RepaintAll();
        }

        private static GameObject FindPreviewAnimator()
        {
            if (Selection.activeGameObject != null)
            {
                var selected = TryGetAnimator(Selection.activeGameObject);
                if (selected != null) return selected;
            }

            if (Player.Instance != null) return TryGetAnimator(Player.Instance.gameObject);
            return null;
        }

        private static GameObject TryGetAnimator(GameObject go)
        {
            var animator = go != null ? go.GetComponentInChildren<Animator>(true) : null;
            return animator != null ? animator.gameObject : null;
        }

        #endregion

        #region 工具

        private float TimeToX(Rect fullRect, float time) => fullRect.x + TrackHeaderWidth + time * _pixelsPerSecond;

        private float XToTime(Rect fullRect, float x) => (x - fullRect.x - TrackHeaderWidth) / _pixelsPerSecond;

        private static float SnapTime(float time)
        {
            const float step = 0.01f;
            return Mathf.Max(0f, Mathf.Round(time / step) * step);
        }

        /// <summary>资源地址支持 "Assets/..." 完整路径，也支持 YooAsset 文件名地址（按文件名在工程内查找）</summary>
        private static UnityEngine.Object ResolveAssetByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            var direct = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (direct != null) return direct;

            string fileName = System.IO.Path.GetFileName(path);
            if (string.IsNullOrEmpty(fileName)) return null;

            var guids = AssetDatabase.FindAssets(fileName + " t:Object");
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (System.IO.Path.GetFileName(assetPath) == fileName)
                {
                    return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                }
            }

            return null;
        }

        private static float ChooseTickStep(float duration)
        {
            float raw = duration / 10f;
            float[] steps = { 0.01f, 0.02f, 0.05f, 0.1f, 0.2f, 0.25f, 0.5f, 1f, 2f, 5f };
            foreach (float s in steps)
            {
                if (raw <= s) return s;
            }
            return steps[steps.Length - 1];
        }

        private static Color GetEventColor(SkillEventType type)
        {
            switch (type)
            {
                case SkillEventType.PlaySound: return new Color(0.3f, 0.7f, 1f);
                case SkillEventType.SpawnVfx: return new Color(0.5f, 1f, 0.5f);
                case SkillEventType.Damage: return new Color(1f, 0.35f, 0.35f);
                case SkillEventType.CameraShake: return new Color(1f, 0.8f, 0.3f);
                case SkillEventType.Motion: return new Color(0.8f, 0.6f, 1f);
                default: return Color.gray;
            }
        }

        private GUIStyle GetLabelStyle()
        {
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.88f, 0.88f, 0.88f) },
                    clipping = TextClipping.Clip,
                };
            }
            return _labelStyle;
        }

        #endregion
    }
}
