


[System.Serializable]
public class SkillAnimationEvent
{
    public string eventName;   // 事件名称
    public float time;         // 触发时间（秒），相对于动画开始
    public bool isTriggered;   // 运行时标记，防止重复触发
}
