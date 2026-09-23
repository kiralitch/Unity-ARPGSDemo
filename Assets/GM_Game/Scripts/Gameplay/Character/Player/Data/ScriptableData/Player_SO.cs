using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * 玩家脚本通用数据
 * 
 */

[CreateAssetMenu(fileName = "Player", menuName = "Custom/Characters/Player")]
public class Player_SO : ScriptableObject
{
    [field: SerializeField] 
    public Player_GroundedData GroundedData { get; private set; }
    
}
