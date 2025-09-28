using UnityEngine;
using UnityEngine.InputSystem;

public class MenuMovement : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float offset = 1f;
    public float time = .3f;
    private Vector2 start;
    private Vector3 velocity;


    void Start()
    {
        start = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 target = Camera.main.ScreenToViewportPoint(Mouse.current.position.ReadValue());
        transform.position = Vector3.SmoothDamp(transform.position, start + (target * offset), ref velocity, time);

    }
}
