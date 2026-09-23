

/*
 * 通用伤害接口
 * 
 */

public interface IDamageable
{
    //TODO 传入Executor执行者的目的是为了后续做受击后面将受击者的位置面向执行者的位置
    void TakeDamage(float damage, CommonActor Executor);
    void PlayHitReaction(CommonActor Executor);
}
