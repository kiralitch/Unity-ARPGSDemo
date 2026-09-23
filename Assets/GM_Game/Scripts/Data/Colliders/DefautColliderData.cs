using System;
using UnityEngine;

[Serializable]
public class DefautColliderData
{
    [field: SerializeField]
    public float Height { get; private set; } = 0.9f;

    [field: SerializeField] 
    public float Radius { get; private set; } = 0.16f;

    [field: SerializeField] 
    public float CenterY { get; private set; } = 0.45f;
}
