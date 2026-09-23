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
        
        CombatStateMachine.playerRef.CurrentStateMode = PlayStateMode.Movement;
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
        MovementStateMachine.ChangeState(MovementStateMachine.IdleState);
        MovementStateMachine.playerRef.CurrentStateMode = PlayStateMode.Movement;
    }
    
}
