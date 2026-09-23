using System;
using UnityEngine;

[Serializable]
public class Player_RotationData
{
    [field:SerializeField]
    public Vector3 TargetRotationReachTime { get; private set; }
}
