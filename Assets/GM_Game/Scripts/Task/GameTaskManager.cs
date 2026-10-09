using System;
using System.Collections;
using Game.Task.Core;
using Game.Task.Dialogue;
using Game.Task.UI;
using UnityEngine;

public class GameTaskManager : MonoBehaviour
{
    private void Awake()
    {
        EnsureManager();
    }

    private void Start()
    {
        StartCoroutine(DelayInitManger());
    }

    private IEnumerator DelayInitManger()
    {
        yield return new WaitForSeconds(0.5f);
        
        // 初始化任务系统
        TaskManager.Instance.Init(() =>
        {
            SetupUI();
        });
    }

    private void EnsureManager()
    {
        if (TaskManager.Instance != null) return;

        var go = new GameObject("TaskManager");
        go.AddComponent<TaskManager>(); // TaskManager.Awake 内部 DontDestroyOnLoad
    }
    
    private void SetupUI()
    {
        //TODO
        
        /*// 任务日志
        if (taskLogView != null)
        {
            _logController = new TaskLogController(taskLogView);
            _logController.Bind();
        }*/

        /*// NPC 对话
        if (dialoguePanel != null)
        {
            _dialogueService = new NpcTaskDialogueService(dialoguePanel);
            dialoguePanel.BindService(_dialogueService);
        }*/
    }
}
