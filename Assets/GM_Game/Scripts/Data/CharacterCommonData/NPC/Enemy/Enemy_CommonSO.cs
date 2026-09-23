using UnityEngine;

[CreateAssetMenu(fileName = "EnemyCommonData", menuName = "Custom/Characters/NPC/Enemy")]
public class Enemy_CommonSO : ScriptableObject
{
    [field:SerializeField]
    public Enemy_CommonData EnemyCommonData { get; private set; }
}
