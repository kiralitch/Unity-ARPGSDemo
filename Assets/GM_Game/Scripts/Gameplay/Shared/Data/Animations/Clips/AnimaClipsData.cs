using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/*
 * 用于存放角色动画
 * 
 */

[Serializable]
public class AnimaClipsData
{
    [field:SerializeField]
    public List<AnimationClip> MovementClips { get; private set; }
    //[field:SerializeField]
    //public List<AnimationClip> AttackClips { get; private set; }
    [field:SerializeField]
    public List<AnimationClip> HitReactionClips { get; private set; }
    [field:SerializeField]
    public List<AnimationClip> DeathClips { get; private set; }
    
    //Mixer
    public AnimationMixerPlayable MovementMixer { get; private set; }
    public AnimationMixerPlayable AttackMixer { get; private set; }
    public AnimationMixerPlayable CommonReactionMixer { get; private set; }

    public AnimationMixerPlayable RootMixer { get; private set; }

    public AnimationPlayableOutput output { get; private set; }
    
    // 目标权重 原理：首先设置数组缓存动画目标，然后将目前的动画权重与缓存的索引进行lerp平滑处理，最后设置
    public float[] movementTargets { get; private set; }
    public float[] attackTargets{ get; private set; }
    public float[] CommonReactionTargets { get; private set; }
    public float targetMoveWeight{ get; private set; } = 1f;
    public float targetAttackWeight{ get; private set; } = 0f;
    public float targetCommonReactionWeight{ get; private set; } = 0f;

    public float blendSpeed { get; private set; } = 8f; // 过渡速度
    
    //攻击节点
    //攻击端口索引
    private const int attackInputPort = 0;
    private const int attackEndInputPort = 1;

    private PlayableGraph graph;

    private AnimationClipPlayable CachedAttackClipPlayable;
    private AnimationClipPlayable CachedEndClipPlayable;
    
    public PlayableGraph Initialize(Animator animator, PlayableGraph RootGraph)
    {
        if (!RootGraph.IsValid())
        {
            RootGraph = PlayableGraph.Create("PlayerAnimationGraph");
            RootGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        }

        /* 必须在确认图有效之后再记录到字段上
           PlayableGraph 是结构体、按值传递：若在有效性检查之前 this.graph = RootGraph，
           当传入的图已被 Destroy（对象池复用会先 Destroy 再重建），
           这里创建的新图只会赋给局部变量，this.graph 仍指向那张已失效的图，
           后续 PreloadNextEnemyComboIfNeeded 里 graph.IsValid() 恒为 false，
           连招第二段起就不再预缓存 */
        this.graph = RootGraph;

        /* 移动动画 */
        MovementMixer = AnimationMixerPlayable.Create(RootGraph);
        if (MovementClips == null || MovementClips.Count == 0)
        {
            Debug.LogError("移动动画列表为空，无法初始化动画图！");
            return RootGraph;
        }

        foreach (var clip in MovementClips)
        {
            var clipPlayable = AnimationClipPlayable.Create(RootGraph, clip);
            MovementMixer.AddInput(clipPlayable, 0, 0f);
        }
        
        movementTargets = new float[MovementMixer.GetInputCount()];
        movementTargets[0] = 1f; // 默认激活第一个动画
        
        /* 通用动画 */
        CommonReactionMixer = AnimationMixerPlayable.Create(RootGraph);
        //先受击动画后死亡动画
        if (HitReactionClips != null)
        {
            foreach (var clip in HitReactionClips)
            {
                var HitPlayable = AnimationClipPlayable.Create(RootGraph, clip);
                CommonReactionMixer.AddInput(HitPlayable, 0, 0f);
            }
        }

        if (DeathClips != null)
        {
            foreach (var clip in DeathClips)
            {
                var HitPlayable = AnimationClipPlayable.Create(RootGraph, clip);
                CommonReactionMixer.AddInput(HitPlayable, 0, 0f);
            }
        }

        CommonReactionTargets = new float[CommonReactionMixer.GetInputCount()];
        if (CommonReactionTargets.Length > 0) CommonReactionTargets[0] = 1f;
        
        //创建攻击混合器
        /* 注意：AnimationMixerPlayable 的 normalizeWeights 参数在 Unity 2022.3 已废弃，
           传任何值都不会生效，混合器始终按「输入权重加权求和」工作。
           因此端口权重之和必须由代码自己维持在 1 附近，
           UpdateBlend 里的 NormalizeRootWeights / NormalizeAttackWeights 就是做这件事的 */
        AttackMixer = AnimationMixerPlayable.Create(RootGraph, 2);
        attackTargets = new float[2];
        attackTargets[0] = 1f;
        
        //InitializeAttackMixer(graph);
        
        //根混合器连接
        RootMixer = AnimationMixerPlayable.Create(RootGraph, 3); //移动、攻击、通用反应三路
        RootGraph.Connect(MovementMixer, 0, RootMixer, 0);
        RootGraph.Connect(AttackMixer, 0, RootMixer, 1);
        RootGraph.Connect(CommonReactionMixer, 0, RootMixer, 2);
        
        output = AnimationPlayableOutput.Create(RootGraph, "Anima", animator);
        output.SetSourcePlayable(RootMixer);
        
        // 初始权重
        RootMixer.SetInputWeight(0, 1f); // 移动
        RootMixer.SetInputWeight(1, 0f); // 攻击
        RootMixer.SetInputWeight(2, 0f);
        
        Debug.Log("动画初始化成功");
        
        RootGraph.Play();
        
        return RootGraph;
    }
    
    /* 混合更新 */
    public void UpdateBlend(float deltaTime)
    {
        // 更新移动混合器
        for (int i = 0; i < MovementMixer.GetInputCount(); i++)
        {
            float current = MovementMixer.GetInputWeight(i);
            float target = movementTargets[i];
            MovementMixer.SetInputWeight(i, Mathf.Lerp(
                current,
                target,
                1f - Mathf.Exp(-blendSpeed * deltaTime))
            );
        }

        for (int i = 0; i < CommonReactionMixer.GetInputCount(); i++)
        {
            float current = CommonReactionMixer.GetInputWeight(i);
            float target = CommonReactionTargets[i];
            CommonReactionMixer.SetInputWeight(i ,Mathf.Lerp(
                current,
                target,
                1f - Mathf.Exp(-blendSpeed * deltaTime)
                ));
        }

        // 更新根混合器
        float currentMove = RootMixer.GetInputWeight(0);
        float currentAttack = RootMixer.GetInputWeight(1);
        float CurrentCommonReacation = RootMixer.GetInputWeight(2);

        float t = 1f - Mathf.Exp(-blendSpeed * deltaTime);

        float newMove = Mathf.Lerp(currentMove, targetMoveWeight, t);
        float newAttack = Mathf.Lerp(currentAttack, targetAttackWeight, t);
        float newCommon = Mathf.Lerp(CurrentCommonReacation, targetCommonReactionWeight, t);

        /* 混合器本身不会归一化权重（normalizeWeights 参数已废弃且无效），
           三个权重之和必须由代码维持：和大于 1 会让动画过冲，
           和小于 1 会让混合结果向绑定姿势收敛（骨骼被拉长）。
           AttackMixer 同理，两个端口权重在这里一并归一化 */
        NormalizeRootWeights(ref newMove, ref newAttack, ref newCommon);

        RootMixer.SetInputWeight(0, newMove);
        RootMixer.SetInputWeight(1, newAttack);
        RootMixer.SetInputWeight(2, newCommon);

        NormalizeAttackWeights();

        //端口重建与上面的权重写入错开一帧，避免同帧重建导致的绑定姿势闪烁
        PreloadNextEnemyComboIfNeeded();
    }

    /* 把根混合器的三个权重按比例缩放到总和为 1 */
    private void NormalizeRootWeights(ref float move, ref float attack, ref float common)
    {
        float Sum = move + attack + common;

        //三者都接近 0 时说明配置异常，退回到纯移动，避免出现 0 权重导致绑定姿势
        if (Sum < 0.0001f)
        {
            move = 1f;
            attack = 0f;
            common = 0f;
            return;
        }

        move /= Sum;
        attack /= Sum;
        common /= Sum;
    }

    /* 攻击混合器两个端口的权重同样必须归一化，否则端口权重和为 0 时
       混合结果会退化成绑定姿势 */
    private void NormalizeAttackWeights()
    {
        float w0 = AttackMixer.GetInputWeight(attackInputPort);
        float w1 = AttackMixer.GetInputWeight(attackEndInputPort);
        float Sum = w0 + w1;

        if (Sum < 0.0001f) return;

        AttackMixer.SetInputWeight(attackInputPort, w0 / Sum);
        AttackMixer.SetInputWeight(attackEndInputPort, w1 / Sum);
    }

    /* 播放攻击动画
     * AttackMixer 是权重混合器，端口必须始终保持有效连接，一旦某个端口为空，
     * 混合结果会退化成绑定姿势（T-pose）。因此这里重建 Playable 并在同一帧内
     * 完成「断开旧连接 -> 连接新动画 -> 切换权重」，中间不执行求值，端口不会出现空档。*/
    public void PlayAttackClip(AnimationClip attackClip, AnimationClip EndClip, PlayableGraph graph, float speed = 1f)
    {
        if (attackClip == null || EndClip == null) return;
        if (!AttackMixer.IsValid())
        {
            Debug.LogError("AttackMixer未初始化！");
            return;
        }

        CachedAttackClipPlayable = CreateAttackPlayable(graph, attackClip, attackInputPort, speed);
        CachedEndClipPlayable = CreateAttackPlayable(graph, EndClip, attackEndInputPort, speed);

        //两个端口都已持有有效动画后再切换权重
        AttackMixer.SetInputWeight(attackInputPort, 1f);
        AttackMixer.SetInputWeight(attackEndInputPort, 0f);
        SetRootTarget(0f, 1f, 0f); // 切换到攻击权重
    }
    
    /* 播放攻击停止动画
     * 只做同帧权重交接：先让收招端口拿到权重，再释放攻击端口权重，
     * 中间不存在端口为空或权重和为 0 的帧 */
    public void StartAttackEnd(PlayableGraph graph, float speed = 1f)
    {
        if (!AttackMixer.IsValid())
        {
            Debug.LogError("AttackMixer未初始化！");
            return;
        }
        if (!CachedEndClipPlayable.IsValid()) return;

        CachedEndClipPlayable.SetSpeed(speed);
        CachedEndClipPlayable.SetTime(0);
        CachedEndClipPlayable.SetDone(false);

        AttackMixer.SetInputWeight(attackEndInputPort, 1f);
        AttackMixer.SetInputWeight(attackInputPort, 0f);

        SetRootTarget(0f, 1f, 0f); // 保持攻击权重
    }

    /* 在指定端口上设置攻击动画，并把播放时间与播放结束标记复位 */
    private AnimationClipPlayable CreateAttackPlayable(
        PlayableGraph graph,
        AnimationClip clip,
        int port,
        float speed)
    {
        var playable = AnimationClipPlayable.Create(graph, clip);
        playable.SetSpeed(speed);
        playable.SetTime(0);
        playable.SetDone(false);

        //先断开旧连接（不销毁），参数槽位立刻被新节点占用
        var Previous = AttackMixer.GetInput(port);
        bool bHasPrevious = Previous.IsValid() && Previous.GetPlayableType() == typeof(AnimationClipPlayable);

        if (Previous.IsValid()) graph.Disconnect(AttackMixer, port);

        graph.Connect(playable, 0, AttackMixer, port);

        //新节点已接管该端口，此时回收旧节点不会再造成空档
        if (bHasPrevious) Previous.Destroy();

        return playable;
    }

    /* 设置移动动画混合 先将全部动画设置为0，然后将目标设置为1 */
    public void SetMovementTarget(int index, float weight = 1f)
    {
        if (index < 0 || index >= movementTargets.Length) return;
        for (int i = 0; i < movementTargets.Length; i++) movementTargets[i] = 0f;
        movementTargets[index] = weight;
        
        ResetAnimationTime(index);
    }
    
    /* 设置通用动画混合 先将全部动画设置为0，然后将目标设置为1 */
    public void SetCommonReacitonTarget(int index, float weight = 1f)
    {
        if (index < 0 || index >= CommonReactionTargets.Length) return;
        for (int i = 0; i < CommonReactionTargets.Length; i++) CommonReactionTargets[i] = 0f;
        CommonReactionTargets[index] = weight;
        
        ResetCommonReactionAnimationTime(index);
    }
    
    public void SetRootTarget(float move, float attack, float common)
    {
        targetMoveWeight = move;
        targetAttackWeight = attack;
        targetCommonReactionWeight = common;
    }

    /* 重置动画时间 */
    private void ResetAnimationTime(int index)
    {
        var clip = MovementMixer.GetInput(index);
        if (clip.IsValid() && clip.GetPlayableType() == typeof(AnimationClipPlayable))
            ((AnimationClipPlayable)clip).SetTime(0);
    }
    
    public void ResetAttackAnimationTime(int index)
    {
        var clip = AttackMixer.GetInput(index);
        if (clip.IsValid() && clip.GetPlayableType() == typeof(AnimationClipPlayable))
            ((AnimationClipPlayable)clip).SetTime(0);
    }
    
    public void ResetCommonReactionAnimationTime(int index)
    {
        var clip = CommonReactionMixer.GetInput(index);
        if (clip.IsValid() && clip.GetPlayableType() == typeof(AnimationClipPlayable))
            ((AnimationClipPlayable)clip).SetTime(0);
    }
    
    /*/* 初始化攻击混合器 #1#
    private void InitializeAttackMixer(PlayableGraph graph)
    {
        AttackMixer = AnimationMixerPlayable.Create(graph, 1); // 只有一个输入
        graph.Connect(AttackMixer, 0, RootMixer, 1); // 连接到根混合器的输入1
        AttackMixer.SetInputWeight(attackInputPort, 0f); // 初始权重0
    }*/

    #region 敌人专用

    /* 当前正在使用的敌人能力数据，直到下一次传入新的技能数据前一直沿用 */
    private EnemyAbilityData CurrentEnemyAbility;

    /* 当前连招段索引，指向 AttackClips 中正在播放的那一段 */
    private int EnemyComboIndex;

    /* 当前正在播放的端口（0 或 1），另一端口用于预缓存下一段连招 */
    private int EnemyComboActivePort = attackInputPort;

    /* 预缓存端口上已经准备好了下一段连招 */
    private bool bEnemyComboPreloaded;

    /* 腾空端口待预缓存：端口重建与权重交接错开一帧执行 */
    private bool bHasPendingPreload;
    private int PendingPreloadPort = -1;

    /* 当前连招段索引，供状态机查询进度 */
    public int CurrentEnemyComboIndex => EnemyComboIndex;

    /* 当前连招总段数 */
    public int CurrentEnemyComboCount => CurrentEnemyAbility?.AttackClips?.Count ?? 0;

    /* 是否还有下一段连招 */
    public bool bHasNextEnemyCombo => CurrentEnemyAbility != null
                                     && EnemyComboIndex + 1 < CurrentEnemyComboCount;

    /* 预缓存端口索引，始终是当前播放端口的另一侧 */
    private int EnemyComboPreloadPort =>
        EnemyComboActivePort == attackInputPort ? attackEndInputPort : attackInputPort;

    /* 播放敌人连招的第一段 */
    public bool PlayEnemyComboClip(EnemyAbilityData ability, PlayableGraph graph)
    {
        if (ability == null)
        {
            Debug.LogError("敌人技能数据为空，无法播放连招动画！");
            return false;
        }

        if (!AttackMixer.IsValid())
        {
            Debug.LogError("AttackMixer未初始化！");
            return false;
        }

        if (ability.AttackClips == null || ability.AttackClips.Count == 0)
        {
            Debug.LogError($"技能 {ability.skillName} 的连招动画列表为空！");
            return false;
        }

        CurrentEnemyAbility = ability;
        EnemyComboIndex = 0;
        EnemyComboActivePort = attackInputPort;
        bEnemyComboPreloaded = false;

        //第一个端口立刻播放第一段，第二端口预缓存第二段（没有第二段则缓存第一段占位）
        CacheEnemyComboClipAt(graph, EnemyComboActivePort, EnemyComboIndex);
        CacheEnemyComboClipAt(graph, EnemyComboPreloadPort, EnemyComboIndex + 1);

        AttackMixer.SetInputWeight(EnemyComboActivePort, 1f);
        AttackMixer.SetInputWeight(EnemyComboPreloadPort, 0f);
        SetRootTarget(0f, 1f, 0f); // 切换到攻击权重

        return true;
    }

    /* 交换端口，当前段播完后由预缓存段接管。
     * 已是最后一段时返回 false，由调用方决定如何结束攻击 */
    public bool PlayNextEnemyComboClip(PlayableGraph graph)
    {
        if (!bHasNextEnemyCombo) return false;

        //先记下本次要停用（腾空）的端口。必须在交换 ActivePort 之前取，
        //否则 EnemyComboPreloadPort 会翻转成正在播放的端口
        int VacatedPort = EnemyComboActivePort;
        int NextPort = EnemyComboPreloadPort;

        //预缓存段直接接管播放，不需要重新连接，避免切换瞬间出现空端口
        if (!bEnemyComboPreloaded)
        {
            CacheEnemyComboClipAt(graph, NextPort, EnemyComboIndex + 1);
        }

        EnemyComboIndex++;
        EnemyComboActivePort = NextPort;
        bEnemyComboPreloaded = false;

        //权重交接：两个端口在同一帧内写入，和不超过 1
        AttackMixer.SetInputWeight(EnemyComboActivePort, 1f);
        AttackMixer.SetInputWeight(VacatedPort, 0f);

        //标记腾空端口待预缓存，实际重建留到下一帧，避免与权重交接同帧
        PendingPreloadPort = VacatedPort;
        bHasPendingPreload = true;

        SetRootTarget(0f, 1f, 0f); // 保持攻击权重
        return true;
    }

    /* 在安全的帧上补上腾空端口的预缓存。由 UpdateBlend 每帧调用，
       与权重交接错开一帧，避免同帧重建端口导致的绑定姿势闪烁。
       这里用 Initialize 时缓存的 graph，不需要外部再传 */
    private void PreloadNextEnemyComboIfNeeded()
    {
        if (!bHasPendingPreload) return;

        bHasPendingPreload = false;

        if (CurrentEnemyAbility == null) return;
        if (!graph.IsValid()) return;

        //只在端口确实腾空（权重为 0）时才重建，避免打断正在播放的段
        if (PendingPreloadPort < 0 || PendingPreloadPort == EnemyComboActivePort) return;
        if (AttackMixer.GetInputWeight(PendingPreloadPort) > 0.0001f) return;

        CacheEnemyComboClipAt(graph, PendingPreloadPort, EnemyComboIndex + 1);
    }

    /* 在指定端口上覆盖连接一段连招动画，并把播放时间与结束标记复位 */
    private void CacheEnemyComboClipAt(PlayableGraph graph, int port, int index)
    {
        var ComboClips = CurrentEnemyAbility.AttackClips;

        //越界说明没有下一段了，用第一段占位，保证端口永远有效，不会退化成绑定姿势
        bool bHasRealClip = index >= 0 && index < ComboClips.Count;
        int ClipIndex = bHasRealClip ? index : 0;

        CreateAttackPlayable(
            graph,
            ComboClips[ClipIndex],
            port,
            CurrentEnemyAbility.animationSpeed);

        //只有缓存到真正的下一段才算预缓存完成，占位动画不能置位
        if (bHasRealClip && port != EnemyComboActivePort) bEnemyComboPreloaded = true;
    }

    /* 重置连招进度，下一次攻击从第一段开始 */
    public void ResetEnemyCombo()
    {
        EnemyComboIndex = 0;
        EnemyComboActivePort = attackInputPort;
        bEnemyComboPreloaded = false;

        //清掉上一次攻击遗留的待预缓存标记，避免误触发端口重建
        bHasPendingPreload = false;
        PendingPreloadPort = -1;
    }

    /* 当前连招的伤害，供受击方结算时读取 */
    public float GetEnemyAbilityDamage()
    {
        return CurrentEnemyAbility != null ? CurrentEnemyAbility.AbilityDamage : 0f;
    }

    /* 结束敌人攻击
     * 这里不释放端口节点，只把权重清零 */
    public void ClearEnemyCombo(PlayableGraph graph)
    {
        if (!AttackMixer.IsValid()) return;

        //先把攻击整体权重交还给移动动画
        SetRootTarget(1f, 0f, 0f);

        //只摘权重，保持端口有效，避免回落期间混入绑定姿势
        for (int port = 0; port < AttackMixer.GetInputCount(); port++)
        {
            AttackMixer.SetInputWeight(port, 0f);
        }

        CurrentEnemyAbility = null;
        ResetEnemyCombo();
    }

    #endregion

    /* 对象池复用时把整张动画图恢复到「刚出生」的干净状态
       死亡/受击状态会把 CommonReactionTargets 指向死亡动画（角色倒地姿势），
       而 AnimaClipsData 是每个敌人实例自己的运行时数据，进池不会重建
       若不复位：复用取出后新的 CommonReactionMixer 仍会把死亡姿势按权重混进来，
       表现为模型倒在胶囊体后面、半个身子陷进地面，直到攻击把根权重切到攻击层才恢复。
       这里把所有目标权重归零，只留下待机移动姿势 */
    public void ResetForReuse()
    {
        /* 通用反应（受击/死亡）全部清零 */
        if (CommonReactionTargets != null)
        {
            for (int i = 0; i < CommonReactionTargets.Length; i++) CommonReactionTargets[i] = 0f;
        }

        /* 移动权重回到第一段（待机），并清掉攻击权重 */
        if (movementTargets != null)
        {
            for (int i = 0; i < movementTargets.Length; i++) movementTargets[i] = 0f;
            if (movementTargets.Length > 0) movementTargets[0] = 1f;
        }

        if (attackTargets != null)
        {
            for (int i = 0; i < attackTargets.Length; i++) attackTargets[i] = 0f;
            if (attackTargets.Length > 0) attackTargets[0] = 1f;
        }

        /* 根权重交还移动层 */
        targetMoveWeight = 1f;
        targetAttackWeight = 0f;
        targetCommonReactionWeight = 0f;

        /* 直接把当前混合权重也压到位，避免复位后还要过渡几帧才淡出死亡姿势 */
        if (MovementMixer.IsValid())
        {
            for (int i = 0; i < MovementMixer.GetInputCount(); i++)
            {
                MovementMixer.SetInputWeight(i, i == 0 ? 1f : 0f);
            }
        }

        if (CommonReactionMixer.IsValid())
        {
            for (int i = 0; i < CommonReactionMixer.GetInputCount(); i++)
            {
                CommonReactionMixer.SetInputWeight(i, 0f);
            }
        }

        if (AttackMixer.IsValid())
        {
            for (int i = 0; i < AttackMixer.GetInputCount(); i++)
            {
                AttackMixer.SetInputWeight(i, 0f);
            }
        }

        if (RootMixer.IsValid())
        {
            RootMixer.SetInputWeight(0, 1f);
            RootMixer.SetInputWeight(1, 0f);
            RootMixer.SetInputWeight(2, 0f);
        }

        /* 连招进度一并清空 */
        CurrentEnemyAbility = null;
        ResetEnemyCombo();

        /* 各动画时间回到起点，复用后不会从上一世播到的位置接着播 */
        if (MovementMixer.IsValid())
        {
            for (int i = 0; i < MovementMixer.GetInputCount(); i++) ResetAnimationTime(i);
        }
        if (CommonReactionMixer.IsValid())
        {
            for (int i = 0; i < CommonReactionMixer.GetInputCount(); i++) ResetCommonReactionAnimationTime(i);
        }
    }
}
