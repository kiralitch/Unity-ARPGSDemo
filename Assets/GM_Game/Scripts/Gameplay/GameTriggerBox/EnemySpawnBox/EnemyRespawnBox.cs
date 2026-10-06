using System;
using UnityEngine;

/*
 * 检测玩家位置后触发敌人
 * 
 */

public class EnemyRespawnBox : CommonTriggerBox
{
    public int bCanTriggerTime = 3;
    public string EnemyPrefabPath;
    public Transform respawnTransform;

    private static int NowTriggerTimer;

    private void Start()
    {
        NowTriggerTimer = 0;
    }

    protected override void OnTriggerEnter(Collider other)
    {
        //检测是否为玩家
        if (!other.CompareTag("Player")) return;

        if (NowTriggerTimer > bCanTriggerTime) return;

        if (respawnTransform == null)
        {
            Debug.LogError("出生地点未设置！");
            return;
        }

        Vector3 RespawnPositon = respawnTransform.position;
        
        GameEnemyManager.Instance.SpawnEnemy(EnemyPrefabPath, RespawnPositon);

        NowTriggerTimer++;
    }

    protected override void OnTriggerStay(Collider other)
    {
        
    }

    protected override void OnTriggerExit(Collider other)
    {
        
    }
}
