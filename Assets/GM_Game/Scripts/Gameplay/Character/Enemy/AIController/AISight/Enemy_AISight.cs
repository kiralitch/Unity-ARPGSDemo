using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * 敌人AI视野监测
 * 
 */

public class Enemy_AISight
{
    // 返回视野内的玩家 Transform，若没有则返回 null
    public Transform GetPlayerInSight(Transform RefTransform,  float sightRange, float sightAngle, LayerMask obstacleMask)
    {
        //获取敌人周围所有碰撞体
        Collider[] hitColliders = Physics.OverlapSphere(RefTransform.position, sightRange);
        foreach (var collider in hitColliders)
        {
            // 判断是否为玩家
            if (!collider.CompareTag("Player")) continue;

            Transform player = collider.transform;
            Vector3 directionToPlayer = player.position - RefTransform.position;
            float distance = directionToPlayer.magnitude;

            // 玩家是否在敌人前方视野锥内
            //得到敌人朝向与玩家方向之间的角度，角度越小玩家越靠近敌人正前方
            float angle = Vector3.Angle(RefTransform.forward, directionToPlayer);
            if (angle > sightAngle / 2f) continue; //如果玩家不在敌人的半角内则跳过

            // 敌人与玩家之间是否有障碍物
            if (Physics.Raycast(RefTransform.position + Vector3.up * 1.5f, 
                    directionToPlayer.normalized, 
                    distance, 
                    obstacleMask))
            {
                continue; // 有障碍物则跳过
            }

            // 通过所有检测，返回玩家 Transform
            return player;
        }

        return null;
    }
}
