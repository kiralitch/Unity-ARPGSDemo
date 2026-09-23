using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class AbilityDataEditor : EditorWindow
{
    private Player_AbilitySet abilitySet;
    private Vector2 scrollPos;
    private bool[] foldouts = new bool[0]; // 每个技能的折叠状态

    // 时间轴绘制相关
    private const float timelineHeight = 30f;
    private const float eventMarkerSize = 8f;

    [MenuItem("Tools/Skill Editor")]
    public static void ShowWindow()
    {
        GetWindow<AbilityDataEditor>("技能编辑器");
    }

    private void OnGUI()
    {
        // 顶部：选择技能集资产
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("技能集:", GUILayout.Width(50));
        abilitySet = (Player_AbilitySet)EditorGUILayout.ObjectField(abilitySet, typeof(Player_AbilitySet), false);
        if (GUILayout.Button("新建", GUILayout.Width(50)))
        {
            CreateNewAbilitySet();
        }
        EditorGUILayout.EndHorizontal();

        if (abilitySet == null)
        {
            EditorGUILayout.HelpBox("请拖入一个 Player_AbilitySet 资产，或点击“新建”创建。", MessageType.Info);
            return;
        }

        // 使用 SerializedObject 进行编辑
        SerializedObject serializedObject = new SerializedObject(abilitySet);
        SerializedProperty listProp = serializedObject.FindProperty("abilitiesList");

        if (listProp == null)
        {
            EditorGUILayout.HelpBox("无法找到 abilitiesList 字段，请检查脚本字段名。", MessageType.Error);
            return;
        }

        serializedObject.Update();

        // 确保 foldouts 数组长度与列表一致
        if (foldouts.Length != listProp.arraySize)
        {
            bool[] newFoldouts = new bool[listProp.arraySize];
            for (int i = 0; i < newFoldouts.Length && i < foldouts.Length; i++)
                newFoldouts[i] = foldouts[i];
            foldouts = newFoldouts;
        }

        // 滚动区域显示所有技能
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        for (int i = 0; i < listProp.arraySize; i++)
        {
            SerializedProperty abilityProp = listProp.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginVertical("box");
            {
                // 技能标题栏（可折叠）
                EditorGUILayout.BeginHorizontal();
                foldouts[i] = EditorGUILayout.Foldout(foldouts[i], $"技能 {i}", true);
                if (GUILayout.Button("删除", GUILayout.Width(50)))
                {
                    listProp.DeleteArrayElementAtIndex(i);
                    serializedObject.ApplyModifiedProperties();
                    // 删除后重新调整 foldouts
                    bool[] newFoldouts = new bool[listProp.arraySize];
                    for (int j = 0; j < newFoldouts.Length; j++)
                        newFoldouts[j] = j < foldouts.Length ? foldouts[j] : false;
                    foldouts = newFoldouts;
                    return; // 重新绘制
                }
                EditorGUILayout.EndHorizontal();

                if (foldouts[i])
                {
                    // 绘制技能详细字段
                    SerializedProperty nameProp = abilityProp.FindPropertyRelative("skillName");
                    SerializedProperty attackClipsProp = abilityProp.FindPropertyRelative("AttackClips");
                    SerializedProperty attackEndClipsProp = abilityProp.FindPropertyRelative("AttackEndClips");
                    SerializedProperty speedProp = abilityProp.FindPropertyRelative("animationSpeed");
                    SerializedProperty inputProp = abilityProp.FindPropertyRelative("inputAction");
                    SerializedProperty distanceProp = abilityProp.FindPropertyRelative("AttackDistance");
                    SerializedProperty eventsProp = abilityProp.FindPropertyRelative("Events");

                    EditorGUILayout.PropertyField(nameProp, new GUIContent("技能名称"));
                    EditorGUILayout.PropertyField(attackClipsProp, new GUIContent("攻击动画（连段）"), true);
                    EditorGUILayout.PropertyField(attackEndClipsProp, new GUIContent("收招动画"), true);
                    EditorGUILayout.PropertyField(speedProp, new GUIContent("动画速度"));
                    EditorGUILayout.PropertyField(inputProp, new GUIContent("输入动作"));
                    EditorGUILayout.PropertyField(distanceProp, new GUIContent("攻击距离"));

                    // 事件编辑区域
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("动画事件", EditorStyles.boldLabel);

                    // 计算总动画时长（所有攻击动画长度之和，忽略过渡）
                    float totalLength = 0f;
                    for (int k = 0; k < attackClipsProp.arraySize; k++)
                    {
                        var clipProp = attackClipsProp.GetArrayElementAtIndex(k);
                        if (clipProp.objectReferenceValue is AnimationClip clip)
                            totalLength += clip.length;
                    }
                    if (totalLength <= 0f) totalLength = 1f;

                    // 绘制简单时间轴
                    DrawTimeline(eventsProp, totalLength);

                    // 事件列表
                    EditorGUILayout.BeginVertical("box");
                    for (int j = 0; j < eventsProp.arraySize; j++)
                    {
                        SerializedProperty eventProp = eventsProp.GetArrayElementAtIndex(j);
                        SerializedProperty eventNameProp = eventProp.FindPropertyRelative("eventName");
                        SerializedProperty timeProp = eventProp.FindPropertyRelative("time");

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"事件 {j}", GUILayout.Width(50));
                        eventNameProp.stringValue = EditorGUILayout.TextField(eventNameProp.stringValue, GUILayout.Width(120));
                        timeProp.floatValue = EditorGUILayout.Slider(timeProp.floatValue, 0f, totalLength, GUILayout.Width(200));
                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            eventsProp.DeleteArrayElementAtIndex(j);
                            break;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();

                    if (GUILayout.Button("添加事件", GUILayout.Height(20)))
                    {
                        eventsProp.arraySize++;
                        var newEvent = eventsProp.GetArrayElementAtIndex(eventsProp.arraySize - 1);
                        newEvent.FindPropertyRelative("eventName").stringValue = "NewEvent";
                        newEvent.FindPropertyRelative("time").floatValue = 0f;
                    }
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        EditorGUILayout.EndScrollView();

        // 底部：添加新技能按钮
        if (GUILayout.Button("添加技能", GUILayout.Height(30)))
        {
            listProp.arraySize++;
            // 新技能默认折叠展开
            bool[] newFoldouts = new bool[listProp.arraySize];
            for (int j = 0; j < newFoldouts.Length; j++)
                newFoldouts[j] = j == listProp.arraySize - 1 ? true : (j < foldouts.Length ? foldouts[j] : false);
            foldouts = newFoldouts;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawTimeline(SerializedProperty eventsProp, float totalLength)
    {
        Rect timelineRect = GUILayoutUtility.GetRect(50, 40, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(timelineRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));

        // 刻度
        Handles.color = Color.gray;
        for (int i = 0; i <= 10; i++)
        {
            float t = i / 10f * totalLength;
            float x = timelineRect.x + (t / totalLength) * timelineRect.width;
            Handles.DrawLine(new Vector3(x, timelineRect.y, 0), new Vector3(x, timelineRect.yMax, 0));
            Handles.Label(new Vector3(x, timelineRect.yMax - 12, 0), t.ToString("0.0"));
        }

        // 事件标记
        for (int i = 0; i < eventsProp.arraySize; i++)
        {
            SerializedProperty eventProp = eventsProp.GetArrayElementAtIndex(i);
            float time = eventProp.FindPropertyRelative("time").floatValue;
            float x = timelineRect.x + (time / totalLength) * timelineRect.width;
            Rect markerRect = new Rect(x - 4, timelineRect.y + 5, 8, 20);
            EditorGUI.DrawRect(markerRect, Color.red);
            string eventName = eventProp.FindPropertyRelative("eventName").stringValue;
            Handles.Label(new Vector3(x, timelineRect.y - 15, 0), eventName);
        }
    }

    private void CreateNewAbilitySet()
    {
        string path = EditorUtility.SaveFilePanelInProject("创建技能集", "NewPlayerAbilitySet", "asset", "请选择保存位置");
        if (string.IsNullOrEmpty(path)) return;

        var newSet = ScriptableObject.CreateInstance<Player_AbilitySet>();
        AssetDatabase.CreateAsset(newSet, path);
        AssetDatabase.SaveAssets();
        abilitySet = newSet;
        EditorGUIUtility.PingObject(newSet);
    }
}