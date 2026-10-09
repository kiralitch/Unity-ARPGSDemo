using System;
using System.Collections.Generic;
using Game.Task.Completion;
using Game.Task.Config;
using Game.Task.Events;
using Game.Task.Rewards;
using Game.Task.Save;
using UnityEngine;

/*
 * 任务系统核心管理器
 *
 * 职责：
 * - 加载任务配置 JSON，构建 TaskDatabase
 * - 管理任务池与进行中任务实例
 * - 监听怪物死亡事件，对“进行中”任务并行计数（不每帧轮询）
 * - 完成判定与完整完成流程（空奖励、回池、存档、弹窗队列）
 * - 存档读写与配置重绑
 *
 * 由 TaskBootstrap 创建并 DontDestroyOnLoad，跨场景持久化。
 */

namespace Game.Task.Core
{
    public class TaskManager : MonoBehaviour
    {
        public static TaskManager Instance { get; private set; }

        // ---- 配置 ----
        private TaskDatabase _database = new TaskDatabase();
        public TaskDatabase Database => _database;

        // ---- 运行时数据 ----
        private readonly TaskPool _pool = new TaskPool();
        private readonly List<TaskInstance> _inProgress = new List<TaskInstance>();

        public TaskPool Pool => _pool;
        public IReadOnlyList<TaskInstance> InProgressTasks => _inProgress;

        // ---- 策略与服务 ----
        private readonly List<ITaskCompletionStrategy> _strategies = new List<ITaskCompletionStrategy>();
        private ITaskRewardService _rewardService = new EmptyTaskRewardService();

        // ---- 事件 ----
        /// <summary>任务日志变化（接取 / 完成 / 进度更新）时触发，UI 据此刷新。</summary>
        public event Action OnTaskLogChanged;

        /// <summary>任务完成通知（完成弹窗队列据此入队）。参数为任务名称。</summary>
        public event Action<string> OnTaskCompleted;

        private bool _initialized;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            RegisterStrategies();
        }

        private void OnEnable()
        {
            GameEventBus.OnMonsterDied += HandleMonsterDied;
        }

        private void OnDisable()
        {
            GameEventBus.OnMonsterDied -= HandleMonsterDied;
        }

        private void RegisterStrategies()
        {
            _strategies.Clear();
            _strategies.Add(new AutoCompleteStrategy());
            // TODO 主线扩展：此处追加 NpcSubmitStrategy 等完成策略
        }

        /// <summary>替换奖励服务（预留扩展）。</summary>
        public void SetRewardService(ITaskRewardService service)
        {
            _rewardService = service ?? new EmptyTaskRewardService();
        }

        // =========================================================
        // 初始化 / 存档
        // =========================================================

        /// <summary>
        /// 初始化：加载 JSON 配置 -> 读档恢复（或新建）-> 重绑最新配置。
        /// 由 TaskBootstrap 或登录流程调用；重复调用会被忽略。
        /// </summary>
        public void Init(Action onDone = null)
        {
            if (_initialized)
            {
                onDone?.Invoke();
                return;
            }

            JsonTaskLoader.LoadAsync(db =>
            {
                if (db == null)
                {
                    Debug.LogError("[Task] 任务配置加载失败，任务系统使用空配置。");
                    db = new TaskDatabase();
                }

                _database = db;
                RestoreFromSave();
                _initialized = true;

                Debug.Log($"[Task] 任务系统初始化完成：定义 {_database.Count} 个，池内 {_pool.Count} 个，进行中 {_inProgress.Count} 个。");
                onDone?.Invoke();
            });
        }

        /// <summary>
        /// 读档流程：加载本地存档 -> 恢复任务池 -> 恢复进行中任务 -> 与最新配置重绑 -> 删除已不存在的任务。
        /// 无存档时按配置初始化任务池（首次进入）。
        /// </summary>
        private void RestoreFromSave()
        {
            _pool.Clear();
            _inProgress.Clear();

            var save = TaskSaveService.Load();

            if (save == null)
            {
                // 首次进入：按配置重新初始化任务池
                _pool.Reset(_database.GetDailyPoolTaskIds());
                SaveNow();
                return;
            }

            // 1. 恢复任务池（过滤掉已不存在的定义）
            var poolIds = new List<string>();
            if (save.PoolTaskIds != null)
            {
                foreach (var id in save.PoolTaskIds)
                {
                    if (_database.Contains(id)) poolIds.Add(id);
                }
            }
            _pool.Reset(poolIds);

            // 2. 恢复进行中任务并与最新配置重绑
            if (save.InProgressTasks != null)
            {
                foreach (var saved in save.InProgressTasks)
                {
                    if (saved == null || string.IsNullOrEmpty(saved.TaskId)) continue;

                    var def = _database.Get(saved.TaskId);
                    if (def == null)
                    {
                        // 任务定义已被删除：直接丢弃该进行中任务（要求 11）
                        Debug.Log($"[Task] 任务定义 {saved.TaskId} 已不存在，丢弃进行中实例。");
                        continue;
                    }

                    var instance = new TaskInstance(def);

                    // 按 targetId 保留进度
                    if (saved.Objectives != null)
                    {
                        foreach (var os in saved.Objectives)
                        {
                            foreach (var o in instance.Objectives)
                            {
                                if (o.TargetId == os.TargetId)
                                {
                                    o.CurrentCount = os.CurrentCount;
                                    if (o.CurrentCount > o.RequiredCount) o.CurrentCount = o.RequiredCount;
                                }
                            }
                        }
                    }

                    instance.RebindTo(def);
                    _inProgress.Add(instance);
                }
            }
        }

        /// <summary>立即写档。</summary>
        public void SaveNow()
        {
            var data = new TaskSaveData();
            data.PoolTaskIds = _pool.ToList();

            foreach (var inst in _inProgress)
            {
                var instSave = new TaskInstanceSave { TaskId = inst.TaskId };
                foreach (var o in inst.Objectives)
                {
                    instSave.Objectives.Add(new TaskObjectiveSave
                    {
                        TargetId = o.TargetId,
                        CurrentCount = o.CurrentCount,
                    });
                }
                data.InProgressTasks.Add(instSave);
            }

            TaskSaveService.Save(data);
        }

        // =========================================================
        // 接取
        // =========================================================

        /// <summary>任务池是否为空（NPC 对话据此隐藏接取选项）。</summary>
        public bool IsPoolEmpty()
        {
            return _pool.IsEmpty;
        }

        /// <summary>该任务定义当前是否已经存在进行中实例（要求 9：同一任务定义只有一个实例）。</summary>
        public bool HasInProgress(string taskId)
        {
            foreach (var inst in _inProgress)
            {
                if (inst.TaskId == taskId) return true;
            }
            return false;
        }

        /// <summary>
        /// 从池中随机抽 1 个任务并接取。池空返回 null。
        /// 抽出后：从池移除 -> 创建进行中实例 -> 加入日志 -> 存档。
        /// </summary>
        public TaskInstance AcceptRandomFromPool()
        {
            if (_pool.IsEmpty) return null;

            string taskId = _pool.DrawRandom();
            var def = _database.Get(taskId);

            // 配置缺失兜底：不接取，直接丢弃
            if (def == null)
            {
                Debug.LogWarning($"[Task] 抽到未知任务定义 {taskId}，跳过。");
                return null;
            }

            var instance = new TaskInstance(def);
            _inProgress.Add(instance);

            SaveNow();
            NotifyLogChanged();
            Debug.Log($"[Task] 接取任务：{def.TaskName}({def.TaskId})");

            return instance;
        }

        /// <summary>拒绝接取：不抽取、不改变池。</summary>
        public void Decline()
        {
            // 无需任何操作：池未改变。
        }

        // =========================================================
        // 进度
        // =========================================================

        private void HandleMonsterDied(MonsterDiedEvent evt)
        {
            // 只遍历进行中任务，未接任务不响应（要求 13、14）
            if (_inProgress.Count == 0) return;

            bool anyChanged = false;
            for (int i = 0; i < _inProgress.Count; i++)
            {
                var inst = _inProgress[i];
                if (inst.State != TaskState.InProgress) continue;

                foreach (var o in inst.Objectives)
                {
                    if (o.Type != TaskObjectiveType.Kill) continue;
                    if (o.TargetId != evt.MonsterId) continue;

                    if (o.AddProgress(1)) anyChanged = true;
                }
            }

            if (anyChanged)
            {
                CheckCompletions();
                NotifyLogChanged();
            }
        }

        /// <summary>给指定任务的目标加进度（GM 添加进度用），并触发完成检查。</summary>
        public bool AddProgress(string taskId, int index, int delta)
        {
            if (string.IsNullOrEmpty(taskId)) return false;

            bool changed = false;
            foreach (var inst in _inProgress)
            {
                if (inst.TaskId != taskId) continue;

                if (index >= 0) changed = inst.AddProgressByIndex(index, delta);
                else
                {
                    // index < 0：给该任务所有目标加进度
                    foreach (var o in inst.Objectives)
                    {
                        if (o.AddProgress(delta)) changed = true;
                    }
                }
                break;
            }

            if (changed)
            {
                CheckCompletions();
                NotifyLogChanged();
            }
            return changed;
        }

        /// <summary>给指定任务的目标加进度（按 targetId）。</summary>
        public bool AddProgressByTargetId(string taskId, int targetId, int delta)
        {
            if (string.IsNullOrEmpty(taskId)) return false;

            bool changed = false;
            foreach (var inst in _inProgress)
            {
                if (inst.TaskId != taskId) continue;
                changed = inst.AddProgress(targetId, delta);
                break;
            }

            if (changed)
            {
                CheckCompletions();
                NotifyLogChanged();
            }
            return changed;
        }

        // =========================================================
        // 完成
        // =========================================================

        /// <summary>遍历进行中任务，执行完成策略判定并完成满足条件的任务。</summary>
        private void CheckCompletions()
        {
            // 收集完成后需要移除的任务
            List<TaskInstance> finished = null;

            for (int i = 0; i < _inProgress.Count; i++)
            {
                var inst = _inProgress[i];
                if (inst.State != TaskState.InProgress) continue;

                var def = _database.Get(inst.TaskId);
                if (def == null) continue;

                var strategy = FindStrategy(def);
                if (strategy == null) continue;

                if (strategy.IsCompleted(def, inst))
                {
                    (finished ??= new List<TaskInstance>()).Add(inst);
                }
            }

            if (finished == null) return;

            foreach (var inst in finished)
            {
                CompleteTask(inst);
            }
        }

        private ITaskCompletionStrategy FindStrategy(TaskDefinition def)
        {
            foreach (var s in _strategies)
            {
                if (s.CanHandle(def)) return s;
            }
            // 兜底：使用自动完成
            return new AutoCompleteStrategy();
        }

        /// <summary>
        /// 执行完整完成流程（要求 17、24）：
        /// 空奖励 -> 从日志移除 -> 任务回池 -> 加入完成弹窗队列 -> 存档。
        /// </summary>
        public void CompleteTask(TaskInstance inst)
        {
            if (inst == null) return;

            var def = _database.Get(inst.TaskId);
            inst.State = TaskState.Completed;

            // 1. 空奖励流程
            _rewardService?.GrantReward(def);

            // 2. 从任务日志移除（进行中列表）
            _inProgress.Remove(inst);

            // 3. 任务回池（可立刻再被接取）
            _pool.Return(inst.TaskId);

            // 4. 加入完成弹窗队列
            string taskName = def != null ? def.TaskName : inst.TaskId;
            OnTaskCompleted?.Invoke(taskName);

            // 5. 存档
            SaveNow();
            NotifyLogChanged();

            Debug.Log($"[Task] 任务完成：{taskName}({inst.TaskId})");
        }

        /// <summary>强制完成（GM）：跳过目标进度，直接执行完成逻辑。</summary>
        public bool ForceComplete(string taskId)
        {
            foreach (var inst in _inProgress)
            {
                if (inst.TaskId != taskId) continue;
                CompleteTask(inst);
                return true;
            }
            Debug.LogWarning($"[Task] 强制完成失败，未找到进行中任务：{taskId}");
            return false;
        }

        // =========================================================
        // GM：清空 / 打印
        // =========================================================

        /// <summary>清空全部任务数据，并按配置重新初始化任务池（要求 25）。</summary>
        public void ClearAll()
        {
            _inProgress.Clear();
            _pool.Reset(_database.GetDailyPoolTaskIds());

            SaveNow();
            NotifyLogChanged();
            Debug.Log("[Task] 已清空全部任务数据并重新初始化任务池。");
        }

        /// <summary>打印任务池、进行中任务与目标进度（要求 27）。</summary>
        public string DumpState()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("====== 任务系统状态 ======");

            sb.AppendLine($"【任务池】({_pool.Count})");
            foreach (var id in _pool.Ids)
            {
                var def = _database.Get(id);
                sb.AppendLine($"  - {id} ({(def != null ? def.TaskName : "?")})");
            }

            sb.AppendLine($"【进行中】({_inProgress.Count})");
            foreach (var inst in _inProgress)
            {
                var def = _database.Get(inst.TaskId);
                sb.AppendLine($"  - {inst.TaskId} ({(def != null ? def.TaskName : "?")})");
                foreach (var o in inst.Objectives)
                {
                    sb.AppendLine($"      · [{o.Type}] targetId={o.TargetId} {o.CurrentCount}/{o.RequiredCount}");
                }
            }

            string text = sb.ToString();
            Debug.Log(text);
            return text;
        }

        // =========================================================
        // 工具
        // =========================================================

        /// <summary>任务日志数据：仅进行中任务，按序列号排序（要求 16）。</summary>
        public List<TaskInstance> GetSortedLogTasks()
        {
            var list = new List<TaskInstance>(_inProgress);
            list.Sort((a, b) =>
            {
                var da = _database.Get(a.TaskId);
                var db = _database.Get(b.TaskId);
                int sa = da != null ? da.SerialId : int.MaxValue;
                int sb = db != null ? db.SerialId : int.MaxValue;
                return sa.CompareTo(sb);
            });
            return list;
        }

        private void NotifyLogChanged()
        {
            OnTaskLogChanged?.Invoke();
        }
    }
}
