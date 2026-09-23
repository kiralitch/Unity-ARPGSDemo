using System;
using UnityEngine;

[Serializable]
public class Player_AnimationData
{
    [Header("状态变量名")] 
    [SerializeField]
    private string GroundedParameterName = "Grounded";
     [SerializeField]
    private string MovingParameterName = "Moving";
     [SerializeField]
    private string StoppingParameterName = "Stopping";

    [Header("地面状态变量名")]
    [SerializeField]
    private string IdleParameterName = "IsIdling";
    [SerializeField]
    private string WalkParameterName = "IsWalking";
    [SerializeField]
    private string RunParameterName = "IsRunning";
    [SerializeField]
    private string LightStopParameterName = "IsLightStopping";
    
    /* 哈希区域 */
    public int GroundedParameterHash { get; private set; }
    public int MovingParameterHash { get; private set; }
    public int StoppingParameterHash { get; private set; }
    public int IdleParameterHash { get; private set; }
    public int WalkParameterHash { get; private set; }
    public int RunParameterHash { get; private set; }
    public int LightStopParameterHash { get; private set; }

    /* 初始化 */
    public void Initialize()
    {
        GroundedParameterHash = Animator.StringToHash(GroundedParameterName);
        MovingParameterHash = Animator.StringToHash(MovingParameterName);
        StoppingParameterHash = Animator.StringToHash(StoppingParameterName);
        IdleParameterHash = Animator.StringToHash(IdleParameterName);
        WalkParameterHash = Animator.StringToHash(WalkParameterName);
        RunParameterHash = Animator.StringToHash(RunParameterName);
        LightStopParameterHash = Animator.StringToHash(LightStopParameterName);
    }

}
