using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/*
 * 按钮点击动效
 * 
 */

public class ButtonStyle01 : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
{
    [SerializeField, Header("按钮默认缩放")]private float BtnDefautsScale = 1f;
    [SerializeField, Header("按钮按下缩放")]private float BtnDownsScale = 0.85f;

    //鼠标抬起时事件
    public void OnPointerUp(PointerEventData eventData)
    {
        transform.DOScale(BtnDefautsScale, 0.05f);
    }

    //鼠标按下时事件
    public void OnPointerDown(PointerEventData eventData)
    {
        transform.DOScale(BtnDownsScale, 0.05f);
    }
}
