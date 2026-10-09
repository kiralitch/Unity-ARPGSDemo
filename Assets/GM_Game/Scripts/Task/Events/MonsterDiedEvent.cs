using UnityEngine;

/*
 * 怪物死亡全局事件数据
 *
 * 战斗系统在怪物死亡后广播；任务系统监听并对“进行中”任务计数。
 * 不要求击杀者是玩家本人（要求 13）。
 */

namespace Game.Task.Events
{
    public struct MonsterDiedEvent
    {
        public int MonsterId;       //怪物 ID（对应任务目标 TargetId）
        public GameObject Killer;   //击杀者（可为空，不要求是玩家）
        public Vector3 DeathPosition; //死亡位置（预留，扩展用）
        public GameObject Victim;   //死亡对象引用（预留）

        public MonsterDiedEvent(int monsterId, GameObject killer, Vector3 pos, GameObject victim)
        {
            MonsterId = monsterId;
            Killer = killer;
            DeathPosition = pos;
            Victim = victim;
        }
    }
}
