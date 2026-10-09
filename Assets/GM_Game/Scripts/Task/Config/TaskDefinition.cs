using System;
using System.Collections.Generic;

/*
 * 任务配置数据（纯 C# 数据结构，非 ScriptableObject）
 *
 * 运行时来源：JSON（由编辑器从 TaskTableSO 导出）
 * 编辑器来源：TaskTableSO -> TaskDefinition 映射（见 TaskTableSO.ToDefinitions）
 *
 * 设计约束：
 * - 运行时只加载 JSON，不直接加载 SO
 * - 字段尽量保持简单可序列化，方便 JsonUtility 解析
 * - 主线扩展字段（前置条件/链式/等级/时限等）先保留占位，当前不实现
 */

namespace Game.Task.Config
{
    /* 接取方式（当前仅 NPC，其余为预留） */
    public enum TaskAcceptType
    {
        Npc = 0,      //NPC 对话自动接取（当前唯一实现）
        Auto = 1,     //预留：自动接取
        Chain = 2,    //预留：链式任务
    }

    /* 目标类型（当前仅 Kill，其余为预留） */
    public enum TaskObjectiveType
    {
        Kill = 0,       //击杀指定怪物 ID 数量
        Collect = 1,    //预留：收集物品
        Interact = 2,   //预留：交互目标
        Reach = 3,      //预留：到达目标
    }

    /* 完成条件类型（当前仅 Auto，NPC 提交为预留） */
    public enum TaskFinishType
    {
        Auto = 0,        //目标全部完成即自动完成
        SubmitToNpc = 1, //预留：回到 NPC 提交
    }

    /// <summary>
    /// 单个任务目标定义。
    /// </summary>
    [Serializable]
    public class TaskObjectiveDefinition
    {
        public TaskObjectiveType Type = TaskObjectiveType.Kill; //目标类型
        public int TargetId;                                    //目标 ID（怪物 ID，仅用于监听匹配，不显示在 UI）
        public string TargetName = "";                          //目标显示名称（UI 显示用，例如“史莱姆”）
        public int RequiredCount = 1;                           //需要数量
    }

    /// <summary>
    /// 奖励定义（当前仅预留字段与接口，不产生实际奖励内容）。
    /// </summary>
    [Serializable]
    public class TaskRewardDefinition
    {
        public int Exp;                              //预留：经验
        public int Gold;                             //预留：金币
        public List<int> ItemIds = new List<int>();  //预留：物品 ID 列表
        public List<int> ItemCounts = new List<int>();//预留：物品数量列表（与 ItemIds 对齐）
    }

    /// <summary>
    /// 任务定义：一个任务的静态配置。
    /// 注意：此类既用于 SO 编辑器源数据，也用于 JSON 运行时数据。
    /// </summary>
    [Serializable]
    public class TaskDefinition
    {
        // ---- 基础信息 ----

        /// <summary>全局唯一 ID。策划保证唯一；重复时编辑器仅警告不阻塞。</summary>
        public string TaskId = "";

        /// <summary>序列号。任务日志按此固定排序。</summary>
        public int SerialId;

        /// <summary>任务名称（完成弹窗显示“【名称】任务完成”）。</summary>
        public string TaskName = "";

        /// <summary>任务描述。</summary>
        public string Description = "";

        // ---- 接取 / 完成配置 ----

        /// <summary>接取方式。当前固定为 NPC。</summary>
        public TaskAcceptType AcceptType = TaskAcceptType.Npc;

        /// <summary>是否可进入日常任务池。勾选后进入全局任务池。</summary>
        public bool bEnterDailyPool = true;

        /// <summary>完成方式。当前为自动完成。</summary>
        public TaskFinishType FinishType = TaskFinishType.Auto;

        // ---- 内容 ----

        /// <summary>目标列表，多个目标并行计数。</summary>
        public List<TaskObjectiveDefinition> Objectives = new List<TaskObjectiveDefinition>();

        /// <summary>奖励（预留，可为空）。</summary>
        public TaskRewardDefinition Reward = new TaskRewardDefinition();

        // ---- 预留：主线扩展（当前不实现，仅保留数据结构空间） ----
        // TODO 主线扩展：前置任务 ID 列表、链式后继任务 ID、分支条件、等级要求、时限、对话条件、是否可重复等。
        public bool bRepeatable = true;          //预留：是否可重复（当前日常任务恒为可重复）
        public List<string> PreTaskIds = new List<string>(); //预留：前置任务
        public string NextTaskId = "";           //预留：链式后继
        public int RequiredLevel = 0;            //预留：等级条件
    }
}
