#if UNITY_EDITOR
using System.Collections.Generic;
using Game.Task.Config;
using UnityEditor;
using UnityEngine;

/*
 * 任务编辑器窗口
 *
 * 功能（要求 7）：
 * - 管理 TaskTableSO 资产（选择 / 新建）
 * - 新建、删除、编辑任务
 * - 编辑目标列表
 * - 编辑奖励预留字段
 * - 导出 JSON（TaskJsonExporter）
 * - 从 JSON 反向导入（TaskJsonImporter）
 * - 唯一 ID 重复时警告，不阻塞
 */

namespace Game.Task.EditorTools
{
    public class TaskEditorWindow : EditorWindow
    {
        private TaskTableSO _table;
        private Vector2 _scroll;
        private readonly List<bool> _foldouts = new List<bool>();

        [MenuItem("Tools/Task/任务编辑器")]
        public static void ShowWindow()
        {
            GetWindow<TaskEditorWindow>("任务编辑器");
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (_table == null)
            {
                EditorGUILayout.HelpBox("请选择或新建一个 TaskTableSO 资产。", MessageType.Info);
                return;
            }

            DrawActions();

            var tasks = _table.GetTasks();
            SyncFoldouts(tasks.Count);

            EditorGUILayout.Space(4);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            for (int i = 0; i < tasks.Count; i++)
            {
                DrawTask(tasks, i);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("任务表:", GUILayout.Width(50));
            _table = (TaskTableSO)EditorGUILayout.ObjectField(_table, typeof(TaskTableSO), false);

            if (GUILayout.Button("新建", GUILayout.Width(50)))
            {
                CreateNewTable();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawActions()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("新增任务"))
            {
                var tasks = _table.GetTasks();
                tasks.Add(CreateDefaultTask(tasks.Count));
                MarkDirty();
            }

            if (GUILayout.Button("检查重复 ID"))
            {
                TaskJsonExporter.WarnDuplicateIds(_table);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("导出 JSON"))
            {
                TaskJsonExporter.Export(_table, TaskJsonExporter.DefaultOutputPath);
            }

            if (GUILayout.Button("从 JSON 导入"))
            {
                TaskJsonImporter.ImportFromMenu();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTask(List<TaskDefinition> tasks, int index)
        {
            var t = tasks[index];
            if (t == null) return;

            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            _foldouts[index] = EditorGUILayout.Foldout(_foldouts[index],
                string.IsNullOrEmpty(t.TaskName) ? $"<未命名 #{index}>" : $"{t.SerialId}. {t.TaskName}",
                true);

            if (GUILayout.Button("删除", GUILayout.Width(50)))
            {
                tasks.RemoveAt(index);
                _foldouts.RemoveAt(index);
                MarkDirty();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUIUtility.ExitGUI();
                return;
            }
            EditorGUILayout.EndHorizontal();

            if (_foldouts[index])
            {
                EditorGUI.indentLevel++;

                t.TaskId = EditorGUILayout.TextField("任务 ID", t.TaskId);
                t.SerialId = EditorGUILayout.IntField("序列号", t.SerialId);
                t.TaskName = EditorGUILayout.TextField("名称", t.TaskName);
                t.Description = EditorGUILayout.TextArea(t.Description, GUILayout.Height(40));

                t.AcceptType = (TaskAcceptType)EditorGUILayout.EnumPopup("接取方式", t.AcceptType);
                t.FinishType = (TaskFinishType)EditorGUILayout.EnumPopup("完成条件(策略)", t.FinishType);
                t.bEnterDailyPool = EditorGUILayout.Toggle("可进入日常池", t.bEnterDailyPool);

                DrawObjectives(t);
                DrawReward(t);
                DrawReserved(t);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawObjectives(TaskDefinition t)
        {
            EditorGUILayout.LabelField("目标列表", EditorStyles.boldLabel);

            if (t.Objectives == null) t.Objectives = new List<TaskObjectiveDefinition>();

            for (int i = 0; i < t.Objectives.Count; i++)
            {
                var o = t.Objectives[i];
                if (o == null) { o = new TaskObjectiveDefinition(); t.Objectives[i] = o; }

                EditorGUILayout.BeginHorizontal();
                o.Type = (TaskObjectiveType)EditorGUILayout.EnumPopup(o.Type, GUILayout.Width(120));
                EditorGUILayout.LabelField("目标ID", GUILayout.Width(50));
                o.TargetId = EditorGUILayout.IntField(o.TargetId, GUILayout.Width(60));
                EditorGUILayout.LabelField("名称", GUILayout.Width(35));
                o.TargetName = EditorGUILayout.TextField(o.TargetName, GUILayout.Width(80));
                EditorGUILayout.LabelField("数量", GUILayout.Width(35));
                o.RequiredCount = EditorGUILayout.IntField(o.RequiredCount, GUILayout.Width(60));

                if (GUILayout.Button("X", GUILayout.Width(24)))
                {
                    t.Objectives.RemoveAt(i);
                    MarkDirty();
                    GUIUtility.ExitGUI();
                    return;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("添加目标"))
            {
                t.Objectives.Add(new TaskObjectiveDefinition());
                MarkDirty();
            }
        }

        private void DrawReward(TaskDefinition t)
        {
            if (t.Reward == null) t.Reward = new TaskRewardDefinition();

            EditorGUILayout.LabelField("奖励（预留，可空）", EditorStyles.boldLabel);
            t.Reward.Exp = EditorGUILayout.IntField("经验", t.Reward.Exp);
            t.Reward.Gold = EditorGUILayout.IntField("金币", t.Reward.Gold);

            if (t.Reward.ItemIds == null) t.Reward.ItemIds = new List<int>();
            if (t.Reward.ItemCounts == null) t.Reward.ItemCounts = new List<int>();

            EditorGUILayout.LabelField($"物品 ID/数量对：{t.Reward.ItemIds.Count} 项");
            if (GUILayout.Button("添加物品奖励"))
            {
                t.Reward.ItemIds.Add(0);
                t.Reward.ItemCounts.Add(0);
                MarkDirty();
            }

            for (int i = 0; i < t.Reward.ItemIds.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("物品", GUILayout.Width(35));
                t.Reward.ItemIds[i] = EditorGUILayout.IntField(t.Reward.ItemIds[i], GUILayout.Width(60));
                EditorGUILayout.LabelField("x", GUILayout.Width(15));
                int count = i < t.Reward.ItemCounts.Count ? t.Reward.ItemCounts[i] : 0;
                count = EditorGUILayout.IntField(count, GUILayout.Width(60));
                if (i < t.Reward.ItemCounts.Count) t.Reward.ItemCounts[i] = count;

                if (GUILayout.Button("X", GUILayout.Width(24)))
                {
                    t.Reward.ItemIds.RemoveAt(i);
                    if (i < t.Reward.ItemCounts.Count) t.Reward.ItemCounts.RemoveAt(i);
                    MarkDirty();
                    GUIUtility.ExitGUI();
                    return;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        /* 预留：主线扩展字段编辑 */
        private void DrawReserved(TaskDefinition t)
        {
            EditorGUILayout.LabelField("预留（主线扩展，不影响当前逻辑）", EditorStyles.miniBoldLabel);
            t.bRepeatable = EditorGUILayout.Toggle("可重复", t.bRepeatable);
            t.RequiredLevel = EditorGUILayout.IntField("等级要求", t.RequiredLevel);
            t.NextTaskId = EditorGUILayout.TextField("后继任务 ID", t.NextTaskId);
        }

        private void SyncFoldouts(int count)
        {
            while (_foldouts.Count < count) _foldouts.Add(false);
            while (_foldouts.Count > count) _foldouts.RemoveAt(_foldouts.Count - 1);
        }

        private void MarkDirty()
        {
            if (_table == null) return;
            EditorUtility.SetDirty(_table);
            AssetDatabase.SaveAssets();
        }

        private void CreateNewTable()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "新建任务表", "TaskTable", "asset", "选择保存路径");

            if (string.IsNullOrEmpty(path)) return;

            var so = ScriptableObject.CreateInstance<TaskTableSO>();
            AssetDatabase.CreateAsset(so, path);
            AssetDatabase.SaveAssets();
            _table = so;

            Debug.Log($"[Task] 已创建任务表：{path}");
        }

        private TaskDefinition CreateDefaultTask(int index)
        {
            return new TaskDefinition
            {
                TaskId = $"daily_task_{index + 1}",
                SerialId = index + 1,
                TaskName = "新任务",
                Description = "",
                AcceptType = TaskAcceptType.Npc,
                FinishType = TaskFinishType.Auto,
                bEnterDailyPool = true,
                Objectives = new List<TaskObjectiveDefinition>
                {
                    new TaskObjectiveDefinition { Type = TaskObjectiveType.Kill, TargetId = 0, RequiredCount = 5 }
                },
                Reward = new TaskRewardDefinition(),
            };
        }
    }
}
#endif
