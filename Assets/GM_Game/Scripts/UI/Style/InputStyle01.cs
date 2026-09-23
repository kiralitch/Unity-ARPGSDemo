using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

/*
 * 占位符设置
 * 
 */

public class InputStyle01 : MonoBehaviour
{
    [SerializeField, Header("占位符")]private TMP_Text TextPlaceHolder;

    [SerializeField, Header("移动时间")] private float Duration = 1f;
    private float PositionY;
    
    private void Start()
    {
        PositionY = TextPlaceHolder.rectTransform.anchoredPosition.y;
        
        //获取输入框
        TMP_InputField ipt = GetComponent<TMP_InputField>();

        if (!string.IsNullOrEmpty(ipt.text)) //当文本不为空时
        {
            //使用了DOTWeen来解决UI动画问题
            TextPlaceHolder.rectTransform.DOAnchorPosY(PositionY + 20,Duration); //往上移动
        }
        
        //当被选中时 这里用的广播绑定，所有不需要update
        ipt.onSelect.AddListener((string str) =>
        {
            if (string.IsNullOrEmpty(ipt.text))
            {
                TextPlaceHolder.rectTransform.DOAnchorPosY(PositionY + 20,Duration); //往上移动
            }
        });
        
        //当不被选中时
        ipt.onDeselect.AddListener((string str) =>
        {
            if (string.IsNullOrEmpty(ipt.text))
            {
                TextPlaceHolder.rectTransform.DOAnchorPosY(PositionY,Duration); //返回默认位置
            }
        });
    }
}
