using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

/*
 * 改变相机位置
 * 
 */

public class CameraZoom : MonoBehaviour
{
    [SerializeField, Range(1f,2f)]
    private float DefautDistance = 1.5f;
    [SerializeField, Range(0f,10f)]
    private float MinDistance = 0.5f;
    [SerializeField, Range(1f,10f)]
    private float MaxDistance = 10f;
    
    [SerializeField, Range(1f,2f)]
    private float Smothing = 2f;
    [SerializeField, Range(0f,1f)]
    private float ZoomSensitivity = 0.5f;
    
    private float currentTargetDistance; //当前目标距离
    
    private CinemachineFramingTransposer farmingTransposer;
    private CinemachineInputProvider inputProvider;

    private void Awake()
    {
        inputProvider = GetComponent<CinemachineInputProvider>();
        farmingTransposer = GetComponent<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineFramingTransposer>();
        currentTargetDistance = DefautDistance;
    }

    private void Update()
    {
        Zoom();
    }

    private void Zoom()
    {
        float zoomInputValue = inputProvider.GetAxisValue(2) * ZoomSensitivity;
        
        //当前位置加上缩放的值
        currentTargetDistance = Mathf.Clamp(currentTargetDistance + zoomInputValue, MinDistance, MaxDistance);
        float currentDistance = farmingTransposer.m_CameraDistance;

        if (currentDistance == currentTargetDistance) return;
        
        float LerpValue = Mathf.Lerp(currentDistance, currentTargetDistance, Smothing * Time.deltaTime);
        farmingTransposer.m_CameraDistance = LerpValue;
    }
}
