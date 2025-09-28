using UnityEngine;

public class CameraRegister : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        CameraManager.Register(GetComponent<Unity.Cinemachine.CinemachineCamera>());
    }

    // Update is called once per frame
    void OnDisable()
    {
        CameraManager.Unregister(GetComponent<Unity.Cinemachine.CinemachineCamera>());
    }
}
