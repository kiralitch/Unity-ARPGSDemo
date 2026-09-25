using UnityEngine;
using UnityEngine.Playables;

/*
 * 玩家类
 * 
 */

[RequireComponent(typeof(PlayerInput))] //当添加Player组件后也会自动添加PlayerInput组件
public class Player : CommonActor
{
    [field:SerializeField][field:Header("数据")]
    public Player_SO AssetData { get; private set; } //通用数据资产

    [field:SerializeField]
    public Player_CommonSO CommonAssetData { get; private set; }
    
    [field: SerializeField]
    [field: Header("技能资产")]
    public Player_AbilitySet Abilities { get; private set; }

    [field: SerializeField]
    [field: Header("角色骨骼")]
    public Transform CharacterBone { get; private set; }

    [field:SerializeField] [field:Header("胶囊体碰撞数据")]
    public CapsuleColliderUtility ColliderUtility { get; private set; }
    
    [field:SerializeField][field:Header("地面")]
    public Player_LayerData LayerData { get; private set; }
    
    [field: SerializeField][field:Header("动画")]
    public AnimaClipsData AnimationClipData { get; private set; }

    [field: SerializeField] [field: Header("武器碰撞器")]
    public SharedWeaponHitBox PlayerWeaponHitBox { get; private set; }
    
    [field: Header("玩家界面UI")]
    public HUDManager PlayerHUD { get; private set; }

    public Rigidbody RigidBody { get; private set; } //刚体组件
    public PlayerInput playerInput { get; private set; }
    public Animator animator { get; private set; }
    public PlayableGraph AnimationGraph { get; private set; }
    
    public Transform MainCameraTransform { get; private set; } //摄像机位置

    public Player_MovementStateMachine MovementStateMachine;
    public Player_CombatStateMachine CombatStateMachine;

    public PlayStateMode CurrentStateMode = PlayStateMode.Movement;
    
    //TODO 测试
    public static Player Instance { get; private set; }

    private void Awake()
    {
        /* 测试区域 */
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        /* 测试区域 */
        
        MovementStateMachine = new Player_MovementStateMachine(this);
        CombatStateMachine = new Player_CombatStateMachine(this);
        RigidBody = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>(); //从组件中获取
        animator = GetComponentInChildren<Animator>();
        
        ColliderUtility.InitColliderData(gameObject);
        ColliderUtility.CalculateCapsuleColliderDimensions();
        
        MainCameraTransform = Camera.main.transform;

        PlayerHUD = GetComponent<HUDManager>();
        
        //AnimationGraph = AnimationClipData.Initialize(animator, AnimationGraph);
        //CharacterBone = FindCharacterBoneByName(transform, "Bip001");
    }

    private void OnValidate() //在编辑器中更新
    {
        ColliderUtility.InitColliderData(gameObject);
        ColliderUtility.CalculateCapsuleColliderDimensions();
    }
    
    private void Start()
    {
        CommonAssetData.commonData.InitData();
        MovementStateMachine.ChangeState(MovementStateMachine.IdleState);
        //AnimationGraph.Play();
        //if(PlayerWeaponHitBox != null) Debug.Log("碰撞体初始化成功");
    }

    private void Update()
    {
        //if (Input.GetKey(KeyCode.Space)) TakeDamage(1f);
        
        switch (CurrentStateMode)
        {
            case PlayStateMode.Movement:
                MovementStateMachine.HandleInput(); //检测输入
                MovementStateMachine.Update();
                break;
            case PlayStateMode.Combat:
                CombatStateMachine.HandleInput();
                CombatStateMachine.Update();
                break;
        }
    }
    
    private void FixedUpdate()
    {
        switch (CurrentStateMode)
        {
            case PlayStateMode.Movement:
                MovementStateMachine.PhysicsUpdate();
                //BindCombatInputAciton();
                break;
            case PlayStateMode.Combat:
                CombatStateMachine.PhysicsUpdate();
                //UnbindCombatInputAction();
                break;
        }
        
        AnimationClipData.UpdateBlend(Time.deltaTime);
    }
    
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
        MovementStateMachine.OnAnimationTransitionEvent();
        CombatStateMachine.OnAnimationTransitionEvent();
    }

    private void OnEnable()
    {
        AnimationGraph = AnimationClipData.Initialize(animator, AnimationGraph);
        
        BindCombatInputAciton();
    }



    private void OnDisable()
    {
        AnimationGraph.Destroy();

        UnbindCombatInputAction();
    }

    #region 技能绑定/解绑函数
    //通过绑定能力数据里的输入，然后单个检测不同技能不同按键
    private void BindCombatInputAciton()
    {
        if (Abilities == null) return;
        
        foreach (var Ability in Abilities.AbilitiesList)
        {
            if (Ability.inputAction != null && Ability.inputAction.action != null)
            {
                Ability.inputAction.action.started += ctx => OnAttackInput(Ability);
            }
        }
    }

    private void UnbindCombatInputAction()
    {
        foreach (var Ability in Abilities.AbilitiesList)
        {
            if (Ability.inputAction != null && Ability.inputAction.action != null)
            {
                Ability.inputAction.action.started -= ctx => OnAttackInput(Ability);
            }
        }
    }

    private void OnAttackInput(AbilitiesData ability)
    {
        if (CurrentStateMode == PlayStateMode.Movement)
        {
            CurrentStateMode = PlayStateMode.Combat;
            MovementStateMachine.ChangeState(MovementStateMachine.IdleState);
            CombatStateMachine.AttackState.CachedAbilitiesData = ability;
            CombatStateMachine.AttackEndState.CachedAbilitiesData = ability;
            PlayerWeaponHitBox.SetCachedDamage(CalculateDamageTaken(ability)); //将伤害传入碰撞体
            CombatStateMachine.ChangeState(CombatStateMachine.AttackState);
        }
        else if (CurrentStateMode == PlayStateMode.Combat)
        {
            //TODO 攻击预输入处理
        }
    }

    /* 计算伤害 技能伤害 * 玩家伤害倍率 */
    private float CalculateDamageTaken(AbilitiesData ability)
    {
        return ability.AbilityDamage * CommonAssetData.commonData.CommonDamageScales;
    }

    #endregion

    #region 角色模型骨骼

    /* 通过父类查找子类（骨骼） */
    private Transform FindCharacterBoneByName(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            var Result = FindCharacterBoneByName(child, name);
            if(Result != null) return Result;
        }

        return null;
    }

    private Vector3 GetMeshLastPosition()
    {
        if (CharacterBone == null) return new Vector3();
        return CharacterBone.position;
    }
    
    /* 将位置更新为模型位置 */
    private void UpdateRigidBodyToMeshPosition()
    {
        if (CharacterBone == null) return;
        
        Vector3 CurrentPostion = CharacterBone.InverseTransformPoint(CharacterBone.position); //获取模型位置
        Vector3 Delta = CurrentPostion - MovementStateMachine.ReusableData.LastMeshPosition; //位置差值

        Vector3 worldDelta = CharacterBone.TransformDirection(Delta); //转化为世界坐标
        
        worldDelta.y = 0f; //只考虑水平
        if (worldDelta.sqrMagnitude > 0.0001f)
        {
            RigidBody.MovePosition(RigidBody.position + worldDelta);
        }

        MovementStateMachine.ReusableData.LastMeshPosition = CurrentPostion;
    }
    
    #endregion

    #region 接口实现

    public bool bCanNotInterruptAttack { get; private set; }

    public bool SetCanNotInterruptAttack(bool bSet)
    {
        bCanNotInterruptAttack = bSet;
        return bCanNotInterruptAttack;
    }

    public override void TakeDamage(float damage, CommonActor Executor)
    {
        CommonAssetData.commonData.SetCurrentHealthDamage = damage;
        Debug.Log($"{Executor.GetType()}对玩家造成当前伤害： {damage},玩家当前血量：{CommonAssetData.commonData.CurrentHealth}");
        
        if (bCanNotInterruptAttack)
        {
            if (CurrentStateMode == PlayStateMode.Movement)
            {
                PlayHitReaction(Executor);
            }
        }
        else
        {
            PlayHitReaction(Executor);
        }
    }

    public override void PlayHitReaction(CommonActor Executor)
    {
        MovementStateMachine.HitReactionState.SetExecutorRef(Executor);
        MovementStateMachine.ChangeState(MovementStateMachine.HitReactionState);

    }

    public override void PlayDeath()
    {
        base.PlayDeath();
        
    }

    #endregion
    

}
