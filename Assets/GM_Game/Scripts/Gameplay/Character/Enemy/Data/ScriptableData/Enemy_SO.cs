using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemySO", menuName = "Custom/Characters/Enemy")]
public class Enemy_SO : ScriptableObject
{
    [field: SerializeField] 
    public Enemy_GroundData GroundedData { get; private set; }
}
