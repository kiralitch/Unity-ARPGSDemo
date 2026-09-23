using System;
using UnityEngine;

[Serializable]
public class Player_StopData
{
    [field: SerializeField] [field: Range(0f, 15f)]
    public float LightDecelerationForce { get; private set; } = 2f;

    [field: SerializeField] [field: Range(0f, 15f)]
    public float MeddileDecelerationForce { get; private set; } = 2.5f;
}
