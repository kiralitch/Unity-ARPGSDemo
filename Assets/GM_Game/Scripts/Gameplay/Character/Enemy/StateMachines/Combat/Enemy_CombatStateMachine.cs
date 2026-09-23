
/*
 * 敌人攻击状态机
 *
 */

public class Enemy_CombatStateMachine : StateMachine
{
    public Enemy EnemyRef { get; }

    public Enemy_CombatCommonState CombatCommonState { get; }
    public Enemy_AttackState AttackState { get; }

    public Enemy_CombatStateMachine(Enemy enemy)
    {
        EnemyRef = enemy;

        CombatCommonState = new Enemy_CombatCommonState(EnemyRef.MovementStateMachine, this);
        AttackState = new Enemy_AttackState(EnemyRef.MovementStateMachine,this);

    }

}
