using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class SimpleVirusMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float speed = 8f;
    [SerializeField] float directionLineLength = 2f;
    
    [Header("Visual Settings")]
    [SerializeField] Color directionLineColor = Color.yellow;
    [SerializeField] float lineWidth = 0.1f;
    
    private Rigidbody2D rb;
    private Camera mainCamera;
    private Vector2 moveInput;
    private Vector2 mouseDirection = Vector2.right; // Default direction
    private LineRenderer directionLine;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        
        // Set up rigidbody
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearDamping = 0f;
            rb.freezeRotation = true;
        }
        
        // Create direction line for Game view
        CreateDirectionLine();
    }
    
    void Update()
    {
        HandleKeyboardInput();
        HandleMouseInput();
        UpdateDirectionVisual();
    }
    
    void FixedUpdate()
    {
        // Apply movement based on WASD input
        rb.linearVelocity = moveInput * speed;
    }
    
    void HandleKeyboardInput()
    {
        if (Keyboard.current == null) return;
        
        Vector2 input = Vector2.zero;
        
        if (Keyboard.current.wKey.isPressed) input.y += 1;
        if (Keyboard.current.sKey.isPressed) input.y -= 1;
        if (Keyboard.current.aKey.isPressed) input.x -= 1;
        if (Keyboard.current.dKey.isPressed) input.x += 1;
        
        moveInput = input.normalized;
    }
    
    void HandleMouseInput()
    {
        if (Mouse.current == null) return;
        
        // Get mouse position in world coordinates
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mouseWorldPos.z = 0f; // Keep it in 2D
        
        // Calculate direction from player to mouse
        Vector2 direction = (mouseWorldPos - transform.position).normalized;
        
        // Only update direction if mouse is far enough from player
        if (Vector2.Distance(mouseWorldPos, transform.position) > 0.5f)
        {
            mouseDirection = direction;
        }
    }
    
    void CreateDirectionLine()
    {
        // Create a GameObject for the direction line
        GameObject lineObject = new GameObject("DirectionLine");
        lineObject.transform.SetParent(transform);
        lineObject.transform.localPosition = Vector3.zero;
        
        // Add LineRenderer component
        directionLine = lineObject.AddComponent<LineRenderer>();
        directionLine.material = new Material(Shader.Find("Sprites/Default"));
        directionLine.startColor = directionLineColor;
        directionLine.endColor = directionLineColor;
        directionLine.startWidth = lineWidth;
        directionLine.endWidth = lineWidth;
        directionLine.positionCount = 2;
        directionLine.sortingOrder = 10; // Make sure it's on top
    }
    
    void UpdateDirectionVisual()
    {
        if (directionLine == null) return;
        
        // Update line positions
        Vector3 startPos = transform.position;
        Vector3 endPos = transform.position + (Vector3)(mouseDirection * directionLineLength);
        
        directionLine.SetPosition(0, startPos);
        directionLine.SetPosition(1, endPos);
    }
    
    void OnDrawGizmos()
    {
        // Draw the direction line and arrow in the Scene view
        Gizmos.color = directionLineColor;
        
        Vector3 startPos = transform.position;
        Vector3 endPos = transform.position + (Vector3)(mouseDirection * directionLineLength);
        
        // Draw the main line
        Gizmos.DrawLine(startPos, endPos);
        
        // Draw arrow head
        Vector3 arrowHead1 = endPos + (Vector3)(Quaternion.Euler(0, 0, 135) * -mouseDirection * 0.3f);
        Vector3 arrowHead2 = endPos + (Vector3)(Quaternion.Euler(0, 0, -135) * -mouseDirection * 0.3f);
        
        Gizmos.DrawLine(endPos, arrowHead1);
        Gizmos.DrawLine(endPos, arrowHead2);
    }
    
    // Public method to get the current direction (useful for other scripts)
    public Vector2 GetDirection()
    {
        return mouseDirection;
    }
    
    // Public method to check if player is moving
    public bool IsMoving()
    {
        return moveInput.magnitude > 0.1f;
    }
}
