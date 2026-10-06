using UnityEngine;

/*
 * 可池化对象接口
 *
 * 需要「取出时初始化 / 回收时清理」逻辑的预制体实现这个接口。
 * 对象池只负责取出与回收，具体怎么复位交给对象自己，
 * 这样池不用知道每种对象的内部细节（敌人、特效、远程攻击、可交互物都适用）。
 */

public interface IPoolable
{
    /* 从池中取出、被激活后调用。用于复位状态（血量、动画、事件订阅等） */
    void OnPoolGet();

    /* 回收进池、被隐藏前调用。用于清理状态（停协程、解绑事件、清计时等） */
    void OnPoolReturn();
}

/*
 * 便于挂载对象持有池归属信息的组件
 */
public class PooledObject : MonoBehaviour
{
    /* 该实例所属的池，由对象池在创建实例时注入 */
    public ObjectPool OwnerPool { get; internal set; }

    /* 不在池中时返回 false */
    public bool ReturnToPool()
    {
        if (OwnerPool == null) return false;

        OwnerPool.Return(this);
        return true;
    }
}
