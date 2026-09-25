using System;
using UnityEngine;

[Serializable]
public class Player_LayerData
{
    [field:SerializeField]
    public LayerMask GroundLayer { get; private set; }
    
    [field:SerializeField]
    public LayerMask EnemyLayer { get; private set; }
}
