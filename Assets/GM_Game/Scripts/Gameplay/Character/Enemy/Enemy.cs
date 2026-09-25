using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;


public class Enemy : CommonActor
{
    [field:SerializeField]
    public Enemy_CommonSO CommonAssetData { get; private set; }
    
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

    public EnemyStateMode CurrentStateMode { get; private set; }

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
        
        /* 本角色位移完全交给 NavMeshAgent，动画只负责姿势。
           若 Animator 开着 applyRootMotion，动画里的根骨骼位移会直接叠加到
           transform 上，把模型推离碰撞体（表现为模型飘在天上、骨骼被拉长），
           同时和 Agent 的转向互相打架导致抖动，这里强制关掉 */
        if (animator != null)
        {
            animator.applyRootMotion = false;
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

        if (CommonAssetData.EnemyCommonData.CurrentHealth < CommonAssetData.EnemyCommonData.MaxHealth)
        {
            CommonAssetData.EnemyCommonData.CurrentHealth = CommonAssetData.EnemyCommonData.MaxHealth;
        }
    }

    private void Start()
    {
        CurrentStateMode = EnemyStateMode.Movement;
        CommonAssetData.EnemyCommonData.InitData();
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
        AnimationGraph = AnimationClipData.Initialize(animator, AnimationGraph);

        AISightCoroutine = StartCoroutine(AISightCheck());

        RegisterBlackBoard();

        RegisterDeathCallback(true);
    }

    private void OnDisable()
    {
        AnimationGraph.Destroy();
        
        StopCoroutine(AISightCoroutine);
        RegisterDeathCallback(false);
    }

    #region 接口实现

    public override void TakeDamage(float damage, CommonActor Executor)
    {
        CommonAssetData.EnemyCommonData.SetCurrentHealthDamage = damage;
        Debug.Log($"{Executor.GetType()}对敌人造成当前伤害： {damage},敌人当前血量：{CommonAssetData.EnemyCommonData.CurrentHealth}");

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

        /* 必须清掉 Attack 这个黑板值：SetBlackBoardValue 只在值发生变化时回调，
           如果 Attack 一直残留为 true，之后敌人再也无法再次触发攻击回调 */
        AIController.ResetAllBlackboard();

        CombatStateMachine.ChangeState(CombatStateMachine.CombatCommonState);

        AnimationClipData.ClearEnemyCombo(AnimationGraph);
        
        MovementStateMachine.HitReactState.SetExecutorRef(Executor);
        MovementStateMachine.ChangeState(MovementStateMachine.HitReactState);
    }

    private void RegisterDeathCallback(bool bIsRegister)
    {
        if (bIsRegister)
        {
            CommonAssetData.EnemyCommonData.OnDeathCall += PlayDeath;
        }
        else
        {
            CommonAssetData.EnemyCommonData.OnDeathCall -= PlayDeath;
        }
    }

    public override void PlayDeath()
    {
        if (CommonAssetData.EnemyCommonData.CurrentHealth > 0f) return;
        
        base.PlayDeath();
        
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
                CommonSO.GroundedData.CachedPlayerTransform = target;
                AIController.SetSightBlackBoardValue("Idle", false);
                AIController.SetSightBlackBoardValue("ChasePlayer");
            }
            else
            {
                CommonSO.GroundedData.CachedPlayerTransform = null;
                AIController.SetSightBlackBoardValue("ChasePlayer", false);
                AIController.SetSightBlackBoardValue("Idle");
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    #endregion
    
}
