using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    private Transform _mainCamera;

    private void Start()
    {
        if (Camera.main != null) _mainCamera = Camera.main.transform;
    }

    private void LateUpdate()
    {
        transform.forward = _mainCamera.forward;
    }
}