using UnityEngine;

public class EnemyHealthBillBoard : MonoBehaviour
{
    private Camera _camera;

    void Start()
    {
        _camera = Camera.main;
    }

    void LateUpdate()
    {
        if (_camera == null) return;
        // 让血条正面朝向摄像机
        transform.forward = _camera.transform.forward;
    }
}
