using System;
using DG.Tweening;
using TMPro;
using UniRx;
using UnityEngine;
using Random = UnityEngine.Random;

/*
 *  系统提示
 * 
 */

public class SystemTips : MonoBehaviour
{
    [SerializeField, Header("提示框文本")]private TMP_Text Text_Tips;
    [SerializeField, Header("提示词颜色")]private Color TextColor;
    [SerializeField, Header("颜色变化曲线")]private AnimationCurve ColorCurve;
    [SerializeField, Header("移动曲线")]private AnimationCurve MoveCurve;

    
    public void ReflashUI(string TipMessage)
    {
        //SetEase()设置曲线
        Text_Tips.SetText(TipMessage);
        Text_Tips.DOColor(TextColor, 2).SetEase(ColorCurve);

        //这获取当前物体（GameObject）的 RectTransform 组件，以便进行 UI 相关的布局操作
        RectTransform RectTran = transform as RectTransform; 
        //执行 Y 轴移动动画
        RectTran.DOAnchorPosY(
            RectTran.anchoredPosition.y + Random.Range(200, 260), //目标Y值，计算方式为当前Y值 + Random.Range(200, 260)
            2 //在2秒内，UI物体的Y坐标从当前值移动到当前值 + 200~260之间的随机数
            ).SetEase(MoveCurve); //设置动画的缓动曲线
        
        //当动画播放完后定时销毁，使用UniRx插件
        //1.Observable定义一个数据流
        //2.使用Timer定时并传入TimeSpan
        //3.定义TimeSpan静态方法 FromSeconds:以毫秒计算
        //4.使用Subscribe绑定需要定时操作的函数
        Observable.Timer(TimeSpan.FromSeconds(3)).Subscribe(v =>
        {
            Destroy(gameObject); //3秒后销毁
        });
    }
}
