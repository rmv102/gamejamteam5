using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class Trigger : MonoBehaviour
{

    public CinemachineCamera cam1;
    public CinemachineCamera cam2;
    public float delay = 0.25f; // let the brain initialize
    public int startPriority = 20;
    public int destPriority = 30;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (cam1 != null) cam1.Priority = startPriority;
        if (cam2 != null) cam2.Priority = startPriority - 10;
        StartCoroutine(SwitchAfterDelay());
    }

    IEnumerator SwitchAfterDelay()
    {
        yield return new WaitForSeconds(delay);
        if (cam2 != null) cam2.Priority = destPriority; // triggers blend
    }


}
