using System;
using UnityEngine;

[Serializable]
public class Player_WalkData
{
    [field: SerializeField] [field: Range(0f, 5f)]
    public float SpeedModifier { get; private set; } = 0.225f;
    
    
}
