using UnityEngine;

[CreateAssetMenu(fileName = "PlayerCommonData", menuName = "Custom/Characters/PlayerCommonData")]
public class Player_CommonSO : ScriptableObject
{
    [field:SerializeField]
    public Player_CommonData commonData { get; private set; }
}
