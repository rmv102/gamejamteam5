using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    // Drag your player GameObject into this field in the Unity Inspector.
    public Transform target;

    // Adjust this value to make the camera follow more smoothly or more tightly.
    public float smoothSpeed = 0.125f;

    // This offset allows you to fine-tune the camera's position relative to the player.
    // The default -10 on the z-axis is standard for a 2D camera.
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    // LateUpdate is called after all Update functions have been called.
    // This is the best place to put camera code to ensure the target has finished moving for the frame.
    void LateUpdate()
    {
        if (target != null)
        {
            // The position the camera wants to be at.
            Vector3 desiredPosition = target.position + offset;

            // Smoothly move from the camera's current position to the desired position.
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

            // Apply the new position to the camera.
            transform.position = smoothedPosition;
        }
    }
}
