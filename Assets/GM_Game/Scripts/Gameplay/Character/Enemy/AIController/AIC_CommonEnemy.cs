using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIC_CommonEnemy : CommonAIController
{
    public Enemy EnemyRef { get; private set; }
    public Enemy_AISight AISight;
    
    private CommonAIBlackBoard EnemyBlackBoard;
    private Dictionary<string, Action <bool> > blackboardCallbacks = new Dictionary<string, Action <bool> >();

    public override void Initialize(Enemy enemy)
    {
        base.Initialize(enemy);

        AISight = new Enemy_AISight();
        EnemyRef = enemy;

        /* 为每个敌人克隆一份自己的黑板数据。
           AIBlackBoardData 是共享的 ScriptableObject 资产，直接读它的 Values
           会让所有敌人共用同一个黑板列表：一个敌人的 Idle/ChasePlayer/Attack
           变化会被其它敌人读到，表现为多个敌人行为完全同步。
           克隆后每个敌人各写各的黑板，互不影响 */
        if (EnemyRef != null && EnemyRef.AIBlackBoardData != null)
        {
            BlackBoardSO clone = Instantiate(EnemyRef.AIBlackBoardData);
            EnemyBlackBoard = clone != null ? clone.Values : null;
        }
    }

    #region 玩家位置

    /* 当前锁定的玩家（视野检测成功后缓存，丢失视野则为 null）。
       存在本控制器实例上，不写进共享的 Enemy_SO.GroundedData：
       那是 ScriptableObject 资产，多敌人会共用同一个引用，
       后检测到的敌人会把先检测到的覆盖掉，导致追击目标互相串 */
    public Transform CachedPlayerTransform { get; set; }

    /* 判断当前是否持有有效的玩家目标 */
    public bool HasPlayerTarget()
    {
        return CachedPlayerTransform != null;
    }

    /* 获取玩家位置，没有目标时返回 false，Position 为敌人自身位置 */
    public bool TryGetPlayerPosition(out Vector3 Position)
    {
        Transform player = CachedPlayerTransform;

        if (player == null)
        {
            Position = EnemyRef != null ? EnemyRef.transform.position : Vector3.zero;
            return false;
        }

        Position = player.position;

        return true;
    }

    /* 获取从敌人指向玩家的水平方向（已去掉 y 轴），没有目标或完全重合时返回 false */
    public bool TryGetDirectionToPlayer(out Vector3 Direction)
    {
        Direction = Vector3.zero;

        if (EnemyRef == null) return false;

        Vector3 delta = CachedPlayerTransform.position - EnemyRef.transform.position;
        delta.y = 0f;

        if (delta.sqrMagnitude < 0.0001f) return false; //玩家与敌人位置重合，无法得到朝向

        Direction = delta.normalized;

        return true;
    }

    /* 获取敌人与玩家的水平距离（忽略高度差），没有目标时返回 float.MaxValue */
    public float GetDistanceToPlayer()
    {
        if (!TryGetPlayerPosition(out Vector3 PlayerPosition)) return float.MaxValue;

        Vector3 delta = PlayerPosition - EnemyRef.transform.position;
        delta.y = 0f;

        return delta.magnitude;
    }

    /* 玩家是否已进入攻击距离，distance 为攻击判定距离（米） */
    public bool IsPlayerInAttackRange(float distance)
    {
        return GetDistanceToPlayer() <= distance;
    }

    /* 玩家是否已进入攻击距离，并且确实在视野/锁定目标中 */
    public bool IsPlayerInAttackRange(float distance, out float currentDistance)
    {
        currentDistance = GetDistanceToPlayer();

        return HasPlayerTarget() && currentDistance <= distance;
    }

    /* 使用 GroundedData 里配置的 AttackRange 判断玩家是否在攻击距离内 */
    public bool IsPlayerInAttackRange()
    {
        if (EnemyRef == null || EnemyRef.CommonSO == null) return false;

        return IsPlayerInAttackRange(EnemyRef.CommonSO.GroundedData.AttackRange);
    }

    #endregion

    #region 主方法

    /* 攻击进行中标记：被置位时视野协程不再改写 Idle / ChasePlayer 黑板，
       避免攻击期间黑板值被覆盖导致状态机反复切换产生抖动 */
    private bool bIsAttacking;

    /* 由 Enemy 在进入 / 退出攻击状态时调用 */
    public void SetAttacking(bool bAttacking)
    {
        bIsAttacking = bAttacking;
    }

    /* 当前是否处于攻击流程中 */
    public bool IsAttacking()
    {
        return bIsAttacking;
    }

    /* 视野刷新专用：攻击期间忽略，其余情况按输入索引正常设置 */
    public void SetSightBlackBoardValue(string name, bool setbool = true)
    {
        if (bIsAttacking) return;

        SetBlackBoardValue(name, setbool);
    }

    public void RegisterBlackboardCallback(string name, Action<bool> callback)
    {
        blackboardCallbacks[name] = callback;
    }

    public void UnBindAllBlackboardCallback()
    {
        blackboardCallbacks.Clear();
    }

    /* 通过输入索引设置该黑板值，如果注册了回调函数则在设置的时候会调用回调函数 */
    public void SetBlackBoardValue(string name, bool setbool = true)
    {
        if (EnemyBlackBoard == null) return;

        /* 先把本次要设置的值写进黑板并触发回调，清空其他黑板值的操作交给回调内部去做。
           否则 SetBlackBoardValue("Attack", true) 会在调用回调前先把 Attack 重新置为 false，
           回调内部若再次读取该值就会直接返回，导致攻击永远触发不了 */
        bool bFound = false;
        bool bChanged = false;

        for (int i = 0; i < EnemyBlackBoard.BlackboardValues.Count; i++)
        {
            if (EnemyBlackBoard.BlackboardValues[i].ValueName != name) continue;

            Blackboard temp = EnemyBlackBoard.BlackboardValues[i];
            bool previous = temp.bIsOn;

            temp.bIsOn = setbool;
            EnemyBlackBoard.BlackboardValues[i] = temp;

            bFound = true;
            bChanged = previous != setbool; //如果现在的布尔不等于设置的布尔则为true

            break;
        }

        if (!bFound) return;

        //回调内部通过 SetBlackBoardValue 清空其他黑板值，这里不会重复清空
        if (bChanged && blackboardCallbacks.TryGetValue(name, out Action<bool> callback))
        {
            callback?.Invoke(setbool);
        }

        /* 目标值不存在时也应该清掉其他黑板，保证同一时刻只有一个激活值 */
        if (bChanged) ResetAllBlackboardButInputNone(name);
    }

    /* 返回黑板值，默认false */
    public bool GetBlackBoardValue(string name)
    {
        foreach (var value in EnemyBlackBoard.BlackboardValues)
        {
            if(value.ValueName == name) return value.bIsOn;
        }

        return false;
    }

    public void ResetAllBlackboardButInputNone(string name)
    {
        for (int i = 0; i < EnemyBlackBoard.BlackboardValues.Count; i++)
        {
            Blackboard temp = EnemyBlackBoard.BlackboardValues[i];
            if(!temp.bIsOn) continue;
            if(temp.ValueName.Equals(name)) continue;
            
            temp.bIsOn = false;
            EnemyBlackBoard.BlackboardValues[i] = temp;
        }
    }

    /* 将所有黑板设为false */
    public void ResetAllBlackboard()
    {
        for (int i = 0; i < EnemyBlackBoard.BlackboardValues.Count; i++)
        {
            Blackboard temp = EnemyBlackBoard.BlackboardValues[i];
            if(!temp.bIsOn) continue;
            
            temp.bIsOn = false;
            EnemyBlackBoard.BlackboardValues[i] = temp;
        }
    }

    /* 默认设置第一个索引为true */
    public void SetFristValue()
    {
        Blackboard frist = EnemyBlackBoard.BlackboardValues[0];
        frist.bIsOn = true;
        EnemyBlackBoard.BlackboardValues[0] = frist;
    }

    #endregion
}
