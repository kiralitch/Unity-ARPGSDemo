using System;
using UnityEngine;

/*
 * 地面移动数据资产
 * 
 */

[Serializable]
public class Player_GroundedData
{
    [field:SerializeField] [field:Range(0f,10f)]
    public float BaseSpeed { get; private set; } = 2f;
    [field: SerializeField] 
    public AnimationCurve SlopeSpeedAngle { get; private set; }

    [field: SerializeField] 
    public Player_RotationData RotationData { get; private set; }
    [field: SerializeField] 
    public Player_WalkData WalkData { get; private set; }
    [field: SerializeField] 
    public Player_RunData RunData { get; private set; }
    [field: SerializeField]
    public Player_StopData StopData { get; private set; }
    [field: SerializeField]
    public Player_RollingData RollingData { get; private set; } 
}
