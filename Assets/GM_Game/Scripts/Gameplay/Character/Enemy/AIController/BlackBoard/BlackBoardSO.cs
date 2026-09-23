using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AIBlackBoard", menuName = "Custom/AIContoller/BlackBoard")]
public class BlackBoardSO : ScriptableObject
{
    [field: SerializeField]
    public CommonAIBlackBoard Values { get;private set;}
}
