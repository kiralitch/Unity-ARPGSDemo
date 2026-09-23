using System;
using UnityEngine;

[Serializable]
public class Player_RollingData
{
    [field:SerializeField][field:Range(0.5f, 2f)]
    public float SpeedModifier { get; private set; } = 1f;
    
    
}
