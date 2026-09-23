using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyAbilitySet", menuName = "Custom/Ability/Enemy")]
public class Enemy_AbilitySet : ScriptableObject
{
    [SerializeField] private List<EnemyAbilityData> abilitiesList = new List<EnemyAbilityData>();
    public List<EnemyAbilityData> AbilitiesList => abilitiesList;
}
