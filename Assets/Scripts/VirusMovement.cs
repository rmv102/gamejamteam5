using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class VirusMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float speed = 8f;
    [SerializeField] float directionLineLength = 2f;
    [SerializeField] float arrowHeadSize = 0.3f;
    
    [Header("Visual Settings")]
    [SerializeField] Color directionLineColor = Color.yellow;
    [SerializeField] Color arrowColor = Color.red;
    [SerializeField] float lineWidth = 0.1f;
    
    private Rigidbody2D rb;
    private Camera mainCamera;
    private Vector2 moveInput;
    private Vector2 mouseDirection;
    private LineRenderer directionLine;
    private GameObject arrowHead;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        
        // Create the direction line visual
        CreateDirectionLine();
        
        // Set up rigidbody
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearDamping = 0f;
            rb.freezeRotation = true;
        }
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
        
        // Create arrow head
        CreateArrowHead();
    }
    
    void CreateArrowHead()
    {
        // Create a simple triangle for the arrow head
        arrowHead = new GameObject("ArrowHead");
        arrowHead.transform.SetParent(transform);
        arrowHead.transform.localPosition = Vector3.zero;
        
        // Add a SpriteRenderer for the arrow
        SpriteRenderer arrowRenderer = arrowHead.AddComponent<SpriteRenderer>();
        arrowRenderer.color = arrowColor;
        arrowRenderer.sortingOrder = 11; // Above the line
        
        // Create a simple triangle sprite
        Texture2D arrowTexture = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        
        // Draw a simple triangle
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                int index = y * 32 + x;
                
                // Create a triangle pointing right
                if (x > 20 && y >= 16 - (x - 20) && y <= 16 + (x - 20))
                {
                    pixels[index] = Color.white;
                }
                else
                {
                    pixels[index] = Color.clear;
                }
            }
        }
        
        arrowTexture.SetPixels(pixels);
        arrowTexture.Apply();
        
        Sprite arrowSprite = Sprite.Create(arrowTexture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        arrowRenderer.sprite = arrowSprite;
        
        // Scale the arrow
        arrowHead.transform.localScale = Vector3.one * arrowHeadSize;
    }
    
    void UpdateDirectionVisual()
    {
        if (directionLine == null || arrowHead == null) return;
        
        // Update line positions
        Vector3 startPos = transform.position;
        Vector3 endPos = transform.position + (Vector3)(mouseDirection * directionLineLength);
        
        directionLine.SetPosition(0, startPos);
        directionLine.SetPosition(1, endPos);
        
        // Update arrow position and rotation
        arrowHead.transform.position = endPos;
        arrowHead.transform.rotation = Quaternion.LookRotation(Vector3.forward, mouseDirection);
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
