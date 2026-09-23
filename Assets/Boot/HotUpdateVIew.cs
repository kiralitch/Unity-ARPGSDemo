using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
 * 更新UI显示
 * 
 */

public class HotUpdateVIew : MonoBehaviour
{
    [SerializeField, Header("进度条")]private Slider slider;
    [SerializeField, Header("加载显示文字")]private TMP_Text LoadingText;

    private void Start()
    {
        slider.value = 0;
        LoadingText.SetText("开始检测是否有更新...");
    }

    //更新资源时刷新进度条
    public void ReflashUI(float prgsValue, string prgsText)
    {
        slider.value = prgsValue;
        LoadingText.SetText(prgsText);
    }
}
