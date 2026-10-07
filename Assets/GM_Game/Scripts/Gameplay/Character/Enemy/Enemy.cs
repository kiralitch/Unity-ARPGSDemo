using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Playables;

/*
 * 敌人类
 * 
 */

public class Enemy : CommonActor, IPoolable
{
    [field:SerializeField]
    public Enemy_CommonSO CommonAssetData { get; private set; }

    /* 运行时的基础数据 */
    public Enemy_CommonData RuntimeCommonData { get; private set; }
    
    [field:SerializeField][field:Header("移动基础数据")]
    public Enemy_SO CommonSO { get; private set; }
    
    [field: SerializeField][field:Header("动画")]
    public AnimaClipsData AnimationClipData { get; private set; }

    [field: SerializeField][field:Header("节能列表")]
    public Enemy_AbilitySet EnemyAbilitySet { get; private set; }

    public Animator animator { get; private set; }
    public PlayableGraph AnimationGraph { get; private set; }

    public Enemy_MovementStateMachine MovementStateMachine;
    public Enemy_CombatStateMachine CombatStateMachine;

    public EnemyStateMode CurrentStateMode { get; set; }

    [field:SerializeField]
    public Transform HitBoxesTrans { get; private set; }

    [field:Header("AI区域")]
    [field: SerializeField]
    public BlackBoardSO AIBlackBoardData { get; private set; }

    [field: SerializeField]
    public bool bCanNotInterruptAttack = false; //攻击是否能被打断

    public AIC_CommonEnemy AIController { get; private set; }
    public Rigidbody Body { get; private set; }

    /* 旋转用的目标节点（模型根节点），为空时回退到自身 */
    [field: SerializeField] [field:Header("旋转")]
    private Transform RotationRoot;
    
    /* 攻击前朝向玩家的角度平滑速度 */
    private float CurrentAttackRotationVelocity;
    public SharedWeaponHitBox HitBoxUtils { get; private set; }

    public Transform RotationTransform => RotationRoot != null ? RotationRoot : transform;

    private EnemyUIManager UIManager;
    
    /* 在指定时间内平滑地把角色朝向旋转到目标方向，用 ref 保存平滑速度 */
    public void RotateTowards(Vector3 Direction, float ReachTime)
    {
        Direction.y = 0f;
        if (Direction.sqrMagnitude < 0.0001f) return;

        Transform Root = RotationTransform;
        float TargetYAngle = Mathf.Atan2(Direction.x, Direction.z) * Mathf.Rad2Deg;
        float CurrentYAngle = Root.eulerAngles.y;

        float SmoothYAngle = Mathf.SmoothDampAngle(
            CurrentYAngle,
            TargetYAngle,
            ref CurrentAttackRotationVelocity,
            Mathf.Max(ReachTime, 0.0001f)
            );

        Root.rotation = Quaternion.Euler(0f, SmoothYAngle, 0f);
    }

    /* 打断旋转平滑，避免下一次攻击继承上一次的角速度 */
    public void ResetRotationVelocity()
    {
        CurrentAttackRotationVelocity = 0f;
    }

    private Coroutine AISightCoroutine;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        Body = GetComponent<Rigidbody>();
        
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
        
        if (Body != null)
        {
            Body.interpolation = RigidbodyInterpolation.None;
        }

        MovementStateMachine = new Enemy_MovementStateMachine(this);
        CombatStateMachine = new Enemy_CombatStateMachine(this);
        AIController = GetComponent<AIC_CommonEnemy>();
        if (AIController != null)
        {
            AIController.Initialize(this);
            CommonSO.GroundedData.InitializeAgent(AIController.Agent);
        }

        HitBoxUtils = HitBoxesTrans.GetComponentInChildren<SharedWeaponHitBox>();
        UIManager = GetComponent<EnemyUIManager>();
        
        /* 克隆一份运行时血量数据，避免多个敌人共享同一个 SO 资产实例
           必须在任何读写 CurrentHealth / 订阅事件之前完成，
           否则事件会挂到共享资产上，导致一个敌人死亡时所有敌人一起死 */
        var clone = Instantiate(CommonAssetData);
        if (clone != null)
        {
            RuntimeCommonData = clone.EnemyCommonData;
        }

        if (RuntimeCommonData.CurrentHealth < RuntimeCommonData.MaxHealth)
        {
            RuntimeCommonData.CurrentHealth = RuntimeCommonData.MaxHealth;
        }
    }

    private void Start()
    {
        CurrentStateMode = EnemyStateMode.Movement;
        RuntimeCommonData.InitData();
        MovementStateMachine.ChangeState(MovementStateMachine.IdleState);
    }

    private void Update()
    {
        MovementStateMachine.Update();
        CombatStateMachine.Update();
    }

    private void FixedUpdate()
    {
        MovementStateMachine.PhysicsUpdate();
        AnimationClipData.UpdateBlend(Time.deltaTime);
        CombatStateMachine.PhysicsUpdate();
    }

    private void OnEnable()
    {
        /* applyRootMotion 是 Animator 上的序列化属性：SetActive(false) 再 SetActive(true)
           （对象池的取出/回收就是这套流程）时，它会从预制体的序列化值恢复
           因此每次启用都要重新关掉，保证刚体和模型只受 Agent 驱动 */
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        AnimationGraph = AnimationClipData.Initialize(animator, AnimationGraph);

        AISightCoroutine = StartCoroutine(AISightCheck());

        RegisterBlackBoard();

        RegisterDeathCallback(true);
    }

    private void OnDisable()
    {
        if (AnimationGraph.IsValid())
        {
            AnimationGraph.Destroy();
        }
        
        if (AISightCoroutine != null)
        {
            StopCoroutine(AISightCoroutine);
            AISightCoroutine = null;
        }

        RegisterDeathCallback(false);
    }

    #region 接口实现

    public override void TakeDamage(float damage, CommonActor Executor)
    {
        RuntimeCommonData.SetCurrentHealthDamage = damage;
        Debug.Log($"{Executor.GetType()}对敌人造成当前伤害： {damage},敌人当前血量：{RuntimeCommonData.CurrentHealth}");

        if (!RuntimeCommonData.CheckIsHaveHealth()) return;

        if (bCanNotInterruptAttack)
        {
            if (CurrentStateMode == EnemyStateMode.Movement)
            {
                PlayHitReaction(Executor);
            }
        }
        else
        {
            PlayHitReaction(Executor);
        }

    }
    
    /* 受伤后播放受击动画 */
    public override void PlayHitReaction(CommonActor Executor)
    {
        /* 受击（或攻击被打断）会脱离攻击流程，这里解除攻击标记，
           否则一次受击之后敌人会永久卡在攻击保护里不再出手 */
        AIController.SetAttacking(false);

        CombatStateMachine.ChangeState(CombatStateMachine.CombatCommonState);
        
        /* 必须清掉 Attack 这个黑板值：SetBlackBoardValue 只在值发生变化时回调，
           如果 Attack 一直残留为 true，之后敌人再也无法再次触发攻击回调 */
        AIController.ResetAllBlackboard();
        
        AnimationClipData.ClearEnemyCombo(AnimationGraph);
        
        MovementStateMachine.HitReactState.SetExecutorRef(Executor);
        MovementStateMachine.ChangeState(MovementStateMachine.HitReactState);
    }

    private void RegisterDeathCallback(bool bIsRegister)
    {
        if (RuntimeCommonData == null) return;

        if (bIsRegister)
        {
            /* 先减再加以防重复订阅：OnEnable 可能被多次触发，
               重复挂载会让一次死亡回调执行多次 PlayDeath */
            RuntimeCommonData.OnDeathCall -= PlayDeath;
            RuntimeCommonData.OnDeathCall += PlayDeath;
        }
        else
        {
            RuntimeCommonData.OnDeathCall -= PlayDeath;
        }
    }

    /* 播放死亡动画 */
    public override void PlayDeath()
    {
        if (RuntimeCommonData.CurrentHealth > 0f) return;
        
        base.PlayDeath();
        
        AIController.SetAttacking(false);
        AIController.ResetAllBlackboard();

        CombatStateMachine.ChangeState(CombatStateMachine.CombatCommonState);

        if (AISightCoroutine != null)
        {
            StopCoroutine(AISightCoroutine);
            AISightCoroutine = null;
        }

        MovementStateMachine.ChangeState(MovementStateMachine.DeathState);
    }
    
    #endregion

    #region 死亡销毁

    /* 没挂 = 非池对象，死亡走 Destroy；挂了且 OwnerPool 有效 = 还池复用 */
    private PooledObject PooledRef
    {
        get
        {
            if (PooledObjectRef == null) PooledObjectRef = GetComponent<PooledObject>();
            return PooledObjectRef;
        }
    }

    private PooledObject PooledObjectRef;

    public override void DestroyOnDeath()
    {
        base.DestroyOnDeath();

        StopDeathRuntime();
        StartCoroutine(FinishDeathAtEndOfFrame());
    }

    /* 停掉所有还在跑的逻辑：AI 视协程、黑板、寻路与物理
       协程必须在这里停，因为 OnDisable 时 StopCoroutine(null) 会告警，
       而销毁流程结束后协程已经无法再安全访问组件 */
    private void StopDeathRuntime()
    {
        AIController?.SetAttacking(false);
        AIController?.ResetAllBlackboard();

        if (AISightCoroutine != null)
        {
            StopCoroutine(AISightCoroutine);
            AISightCoroutine = null;
        }

        var Agent = AIController != null ? AIController.Agent : null;
        if (Agent != null && Agent.enabled)
        {
            Agent.isStopped = true;
            Agent.enabled = false;
        }

        /* 死亡后不该再参与受击与碰撞,禁掉刚体并关掉所有碰撞体，
           否则尸体在动画结束的这一帧还能被打中并触发一次新的死亡流程 */
        if (Body != null)
        {
            Body.velocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Body.isKinematic = true;
            Body.detectCollisions = false;
        }

        var Colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < Colliders.Length; i++)
        {
            Colliders[i].enabled = false;
        }
    }

    /* 等到当前帧结束再收尾
       是对象池时还给池；非池对象时Destroy */
    private IEnumerator FinishDeathAtEndOfFrame()
    {
        yield return new WaitForEndOfFrame();

        /* 销毁 / 隐藏 GameObject 都会触发 OnDisable，那里会释放 PlayableGraph并解绑死亡回调 */
        if (PooledRef != null && PooledRef.ReturnToPool())
        {
            yield break;
        }

        Destroy(gameObject);
    }

    #endregion

    private void RegisterBlackBoard()
    {
        AIController.RegisterBlackboardCallback("Attack", AttackCallBack);        
    }

    #region 黑板值注册函数

    private void AttackCallBack(bool obj)
    {
        if (!obj) return;
        if (CurrentStateMode == EnemyStateMode.Death) return;
        
        Debug.Log("进入攻击范围");

        EnemyAbilityData ability = GetRandomAbility();
        if (ability == null)
        {
            Debug.LogWarning($"{name} 没有可用的敌人技能数据，无法进入攻击");
            return;
        }

        //MovementStateMachine.ChangeState(MovementStateMachine.IdleState);

        //先占住攻击标记，攻击期间黑板值不再被视野协程和移动状态改写
        AIController.SetAttacking(true);

        //先把技能数据交给攻击状态，再切换状态，保证 Enter 时能拿到数据播放连招
        CombatStateMachine.AttackState.CachedAbilityData = ability;
        CombatStateMachine.ChangeState(CombatStateMachine.AttackState);
    }

    /* 从技能列表里随机取一个可用的技能（连招动画不为空） */
    private EnemyAbilityData GetRandomAbility()
    {
        if (EnemyAbilitySet == null) return null;

        var Abilities = EnemyAbilitySet.AbilitiesList;
        if (Abilities == null || Abilities.Count == 0) return null;

        int StartIndex = UnityEngine.Random.Range(0, Abilities.Count);

        //从随机位置开始找第一个有效技能，避免随机到空数据时要多次重试
        for (int i = 0; i < Abilities.Count; i++)
        {
            var ability = Abilities[(StartIndex + i) % Abilities.Count];

            if (ability == null || ability.AttackClips == null || ability.AttackClips.Count == 0) continue;

            return ability;
        }

        return null;
    }

    #endregion
    
    #region 动画关键帧函数 

    public void OnMovementStateAnimationEnterEvent()
    {
        MovementStateMachine.OnAnimationEnterEvent();
        CombatStateMachine.OnAnimationEnterEvent();
    }
    
    public void OnMovementStateAnimationExitEvent()
    {
        MovementStateMachine.OnAnimationExitEvent();
        CombatStateMachine.OnAnimationExitEvent();
    }
    
    public void OnMovementStateAnimationTransitionEvent()
    {
        //攻击动画的关键帧事件走攻击状态，避免连招过渡帧被移动状态机吞掉
        if (CombatStateMachine.GetCurrentState() == CombatStateMachine.AttackState)
        {
            CombatStateMachine.OnAnimationTransitionEvent();
            return;
        }

        MovementStateMachine.OnAnimationTransitionEvent();
    }

    #endregion

    /* 连招播完由攻击状态回调：清理连招节点、解除攻击保护并回到待机 */
    public void OnAttackFinished()
    {
        //先退出攻击状态，让 AttackState.Exit 复位朝向与 Agent 控制
        CombatStateMachine.ChangeState(CombatStateMachine.CombatCommonState);

        //再解除攻击保护，之后的移动状态切换才有权交还根权重
        AIController.SetAttacking(false);

        //清掉黑板里残留的 Attack 值，否则下次无法再次触发攻击回调
        AIController.ResetAllBlackboard();

        //最后交还动画权重：内部会把根权重设回移动，并回收连招节点
        AnimationClipData.ClearEnemyCombo(AnimationGraph);

        MovementStateMachine.ChangeState(MovementStateMachine.IdleState);
    }

    #region AI控制器函数

    private IEnumerator AISightCheck()
    {
        while (true)
        {
            Transform target = AIController.AISight.GetPlayerInSight(
                transform,
                CommonSO.GroundedData.sightRange,
                CommonSO.GroundedData.sightAngle,
                CommonSO.GroundedData.obstacleMask
                );

            if (target != null)
            {
                AIController.CachedPlayerTransform = target;
                AIController.SetSightBlackBoardValue("Idle", false);
                AIController.SetSightBlackBoardValue("ChasePlayer");
            }
            else
            {
                AIController.CachedPlayerTransform = null;
                AIController.SetSightBlackBoardValue("ChasePlayer", false);
                AIController.SetSightBlackBoardValue("Idle");
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    #endregion

    #region 对象池接口

    /* 从池中取出后复位 */
    public void OnPoolGet()
    {
        if (RuntimeCommonData != null)
        {
            RuntimeCommonData.InitData(); // CurrentHealth = MaxHealth
        }

        /* 兜底再关一次根位移：OnEnable 已经关过，但这里再保证一次，
           避免取出流程有别的路径把它重新打开 */
        if (animator != null) animator.applyRootMotion = false;

        CurrentStateMode = EnemyStateMode.Movement;

        AIController?.SetAttacking(false);
        AIController?.ResetAllBlackboard();
        AIController?.ResetCachedPlayer();

        if(UIManager != null) UIManager.InitHealthBar();

        /* 复位动画图：死亡时把根权重 targets 与通用反应权重都指向了死亡动画（倒地姿势），
           而 AnimaClipsData 是实例自己的运行时数据、进池不重建。
           不复位的话复用后死亡姿势会继续混进待机/跑动，表现为模型倒在胶囊体后面、
           半身陷地，直到攻击把根权重切到攻击层才恢复。
           此时 OnEnable 已经重建好动画图，可以安全复位 */
        AnimationClipData?.ResetForReuse();
        
        /* 还原 AI 寻路
           先把 Agent 的内部位置 Warp 到当前的出生点，再恢复寻路
           复用取出时 Agent 内部还停在上一世死亡的位置，直接启用会先「瞬移」回旧点
           再折返，看起来就是出生后疯跑一段
           注意：Warp 前必须先确保 Agent 处于启用状态且在 NavMesh 上 */
        var Agent = AIController != null ? AIController.Agent : null;
        if (Agent != null)
        {
            Agent.enabled = true;
            Agent.isStopped = true;

            /* Agent 的 transform 已经被池的取出流程摆到出生点，这里同步内部位置 */
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit Hit, 1f, NavMesh.AllAreas))
            {
                Agent.Warp(Hit.position);
            }

            Agent.ResetPath();
            Agent.isStopped = false;
        }

        /* 还原刚体与碰撞 */
        if (Body != null)
        {
            Body.velocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Body.isKinematic = true;
            Body.detectCollisions = true;
            Body.interpolation = RigidbodyInterpolation.None;
        }

        /* 还原自身碰撞体
           武器命中盒不在自身碰撞体里，它由攻击状态在出手瞬间开关，出生时必须是关的 */
        var Colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < Colliders.Length; i++)
        {
            Colliders[i].enabled = true;
        }

        /* 单独关掉武器命中盒：它是 trigger，出生前不该参与命中判定 */
        if (HitBoxUtils != null)
        {
            HitBoxUtils.DisableHitBox();
        }

        ResetRotationVelocity();

        MovementStateMachine?.ChangeState(MovementStateMachine.IdleState);
    }

    /* 回收进池前清理
       SetActive(false) 会触发 OnDisable，里面已负责停视野协程、解绑死亡回调、释放 PlayableGraph，
       这里只做状态层面的兜底，避免残留影响下一次复用 */
    public void OnPoolReturn()
    {
        if (RuntimeCommonData != null)
        {
            RuntimeCommonData.InitData();
        }
        
        AIController?.SetAttacking(false);
        AIController?.ResetAllBlackboard();
        AIController?.ResetCachedPlayer();

        CombatStateMachine?.ChangeState(CombatStateMachine.CombatCommonState);
    }

    #endregion
    
}
