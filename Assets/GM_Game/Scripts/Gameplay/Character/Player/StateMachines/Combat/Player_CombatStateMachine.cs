

/*
 * 玩家攻击状态机
 * 
 */

public class Player_CombatStateMachine : StateMachine
{
    public Player playerRef { get; }

    public Player_CombatCommonState CombatCommonState { get; }
    public Player_AttackState AttackState { get; }
    public Player_AttackEndState AttackEndState { get; }

    public Player_StateReusableData ReusableData { get; } //不需要设置的值 
    
    public Player_CombatStateMachine(Player player)
    {
        playerRef = player;

        CombatCommonState = new Player_CombatCommonState(playerRef.MovementStateMachine, this);
        AttackState = new Player_AttackState(playerRef.MovementStateMachine,this);
        AttackEndState = new Player_AttackEndState(playerRef.MovementStateMachine,this);

        ReusableData = playerRef.MovementStateMachine.ReusableData;
    }
}
