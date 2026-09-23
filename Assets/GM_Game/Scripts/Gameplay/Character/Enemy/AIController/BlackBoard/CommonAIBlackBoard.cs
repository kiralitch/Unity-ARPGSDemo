using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct Blackboard
{
    [field:SerializeField]
    public string ValueName;
    [field:SerializeField]
    public bool bIsOn;
}

[Serializable]
public class CommonAIBlackBoard
{
    [field:SerializeField]
    public List<Blackboard> BlackboardValues { get; private set; }
}
