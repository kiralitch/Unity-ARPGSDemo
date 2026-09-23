using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/*
 * 玩家输入类
 * 
 */

public class PlayerInput : MonoBehaviour
{
    public PlayerInputAction InputActions { get; private set; }
    public PlayerInputAction.PlayerInputActions PlayerActions { get; private set; }
    public PlayerInputAction.PlayerCombatInputActions CombatInputActions { get; private set; }

    private void Awake()
    {
        InputActions = new PlayerInputAction();
        PlayerActions = InputActions.PlayerInput; //实例化
        CombatInputActions = InputActions.PlayerCombatInput;
    }

    /* 在对象激活时可用 */
    private void OnEnable()
    {
        PlayerActions.Enable();
        CombatInputActions.Enable();
    }

    /* 在对象禁用时时不可用 */
    private void OnDisable()
    {
        PlayerActions.Disable();
        CombatInputActions.Disable();
    }

    /* 无法使用输入 */
    public void DisableActionInput(InputAction action, float seconds)
    {
        StartCoroutine(DisableAction(action, seconds)); //开始协程
    }

    /* 协程(只能在MonoBehaviour执行) 延迟多少秒后再执行 */
    private IEnumerator DisableAction(InputAction action, float seconds)
    {
        action.Disable();
        
        yield return new WaitForSeconds(seconds); //等待几秒后在执行下面的函数
        
        action.Enable();
    }
}
