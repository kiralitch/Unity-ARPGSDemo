using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommonBillBoard : MonoBehaviour
{
    private Camera _camera;

    void Start()
    {
        _camera = Camera.main;
    }

    void LateUpdate()
    {
        if (_camera == null) return;
        // 让UI正面朝向摄像机
        transform.forward = _camera.transform.forward;
    }
}
