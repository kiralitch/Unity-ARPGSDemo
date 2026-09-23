
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Player", menuName = "Custom/Ability/Player")]
public class Player_AbilitySet : ScriptableObject
{
    //[field: SerializeField] 
    //public List<AbilitiesData> AbilitiesList { get; private set; }
    
    [SerializeField] private List<AbilitiesData> abilitiesList = new List<AbilitiesData>();
    public List<AbilitiesData> AbilitiesList => abilitiesList;
    
}
