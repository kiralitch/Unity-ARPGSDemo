

/*
 * 状态机接口
 *  
 */

public interface IState
{
    public void Enter(); //进入该状态
    public void Exit(); //退出该状态
    public void HandleInput();
    public void Update();
    public void PhysicsUpdate(); //类似FixUpdate
    public void OnAnimatationEnterEvent(); //类似于动画通知开头
    public void OnAnimatationExitEvent();  //类似于动画通知结束
    public void OnAnimationTransitionEvent();  //从特定帧过渡到其他状态
    
}
