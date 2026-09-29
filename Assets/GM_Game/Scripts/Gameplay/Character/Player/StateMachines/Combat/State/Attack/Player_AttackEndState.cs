using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * 攻击结束状态
 * 
 */

public class Player_AttackEndState : Player_CombatCommonState
{
    public Player_AttackEndState(Player_MovementStateMachine playerMovementStateMachine, Player_CombatStateMachine playerCombatStateMachine) : base(playerMovementStateMachine, playerCombatStateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        
        MovementStateMachine.playerRef.CurrentStateMode = PlayStateMode.Movement;
        PlayAttackEndAnimaiton();
    }

    #region Main

    private void PlayAttackEndAnimaiton()
    {
        if (EndCombotIndex < 0 || EndCombotIndex >= CachedAbilitiesData.AttackClips.Count)
            EndCombotIndex = 0;
        
        CombatStateMachine.playerRef.AnimationClipData.StartAttackEnd(
            CombatStateMachine.playerRef.AnimationGraph
        );

        if (EndCombotIndex < CachedAbilitiesData.AttackClips.Count - 1)
        {
            EndCombotIndex++;
        }
        else
        {
            EndCombotIndex = 0;
        }
    }

    #endregion
    
    public override void OnAnimatationExitEvent()
    {

    }

    public override void OnAnimatationEnterEvent()
    {
        base.OnAnimatationEnterEvent();
    }

    public override void OnAnimationTransitionEvent()
    {
        //收招动画播完才把控制权交还移动状态机。
        //顺序很重要：先切模式（让 Player.FixedUpdate 开始驱动 MovementStateMachine），
        //再切状态，避免出现「模式是 Combat 但移动状态已切走」的空档
        MovementStateMachine.ChangeState(MovementStateMachine.IdleState);
    }
    
}
