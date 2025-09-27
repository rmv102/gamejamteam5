using UnityEngine;
using UnityEngine.InputSystem; // Required for the new Input System

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 8f;
    public float acceleration = 20f;
    public float deceleration = 15f;
    
    [Header("Softbody Settings")]
    public float forceMultiplier = 2f; // Multiplier for forces applied to softbody
    public float maxForce = 50f; // Maximum force that can be applied
    public bool useForceBasedMovement = true; // Toggle between force-based and velocity-based movement
    
    [Header("Dash Force Settings")]
    public float dashForceMultiplier = 5f; // Extra multiplier specifically for dash forces
    public float dashMaxForce = 200f; // Higher max force for dashing
    public bool useVelocityDuringDash = true; // Use direct velocity control during dash for more reliable movement
    
    [Header("Dash Settings")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.3f;
    public float dashCooldown = 2.5f; // Increased from 1f to 2.5f for longer cooldown
    public int maxDashes = 5; // Maximum number of dashes
    public float dashKillRadius = 1.5f; // Radius for killing enemies during dash
    
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 lastMoveInput; // Store last movement direction for dashing
    private Vector2 dashDirection;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private int currentDashes; // Current number of dashes available

    // Start is called before the first frame update
    void Start()
    {
        // Get the Rigidbody2D component attached to this GameObject.
        rb = GetComponent<Rigidbody2D>();
        
        // Configure Rigidbody2D for smooth movement
        if (rb != null)
        {
            rb.gravityScale = 0f; // No gravity for 2D top-down movement
            rb.linearDamping = useForceBasedMovement ? 0.5f : 0f; // Small damping for softbody to prevent oscillation
            rb.angularDamping = 0f; // No rotation damping
            rb.freezeRotation = true; // Prevent rotation
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // Smooth interpolation
            
            // For softbody: slightly higher mass for more stable movement
            if (useForceBasedMovement)
            {
                rb.mass = Mathf.Max(rb.mass, 1f); // Ensure minimum mass
            }
        }
        
        // Initialize dash count
        currentDashes = maxDashes;
        Debug.Log($"Player initialized with {currentDashes} dashes (max: {maxDashes})");
        
        // Optimize spring joints for better responsiveness
        if (useForceBasedMovement)
        {
            OptimizeSpringJoints();
        }
    }
    
    void OptimizeSpringJoints()
    {
        // Find and optimize all spring joints for better responsiveness
        SpringJoint2D[] springJoints = GetComponentsInChildren<SpringJoint2D>();
        
        foreach (SpringJoint2D springJoint in springJoints)
        {
            // Increase frequency for more responsive movement
            springJoint.frequency = Mathf.Max(springJoint.frequency, 5f);
            
            // Reduce damping ratio to prevent over-damping
            springJoint.dampingRatio = Mathf.Min(springJoint.dampingRatio, 0.3f);
            
            // Enable auto-configure to ensure proper setup
            springJoint.autoConfigureConnectedAnchor = true;
        }
        
        Debug.Log($"Optimized {springJoints.Length} spring joints for better responsiveness");
    }

    // Update is called once per frame
    void Update()
    {
        // Get the current keyboard state.
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            // If there's no keyboard, do nothing.
            return;
        }

        // Handle dash cooldown
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }
        
        // Check if all enemies are cleared (reward system)
        CheckForEnemyClear();

        // Check for dash input
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            Debug.Log($"Space pressed! isDashing: {isDashing}, dashCooldownTimer: {dashCooldownTimer:F2}, currentDashes: {currentDashes}");
            
            if (!isDashing && dashCooldownTimer <= 0f && currentDashes > 0)
            {
                StartDash();
            }
            else
            {
                if (isDashing) Debug.Log("Cannot dash: Already dashing");
                if (dashCooldownTimer > 0f) Debug.Log($"Cannot dash: On cooldown ({dashCooldownTimer:F2}s remaining)");
                if (currentDashes <= 0) Debug.Log("Cannot dash: No dashes remaining");
            }
        }

        // Handle dash timer
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                EndDash();
            }
        }

        // Only process movement input if not dashing
        if (!isDashing)
        {
            // Create a vector to store the input.
            Vector2 input = Vector2.zero;

            // Check for WASD and Arrow Key presses.
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                input.y += 1;
            }
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                input.y -= 1;
            }
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                input.x -= 1;
            }
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                input.x += 1;
            }

            // Store the input in a Vector2 and normalize it.
            // Normalizing prevents faster diagonal movement.
            moveInput = input.normalized;
            
            // Store last movement direction for dashing
            if (moveInput.magnitude > 0.1f)
            {
                lastMoveInput = moveInput;
            }
        }
    }

    // FixedUpdate is called on a fixed time interval and is the best place for physics calculations.
    void FixedUpdate()
    {
        if (isDashing)
        {
            // Apply dash velocity with smooth deceleration
            float dashProgress = 1f - (dashTimer / dashDuration);
            float currentDashSpeed = Mathf.Lerp(dashSpeed, 0f, dashProgress * dashProgress); // Quadratic easing for smooth deceleration
            
            if (useForceBasedMovement && !useVelocityDuringDash)
            {
                // For softbody: apply much stronger force for dashing
                Vector2 dashForce = dashDirection * currentDashSpeed * forceMultiplier * dashForceMultiplier;
                dashForce = Vector2.ClampMagnitude(dashForce, dashMaxForce);
                rb.AddForce(dashForce, ForceMode2D.Force);
                
                // Also apply impulse for immediate response
                Vector2 impulseForce = dashDirection * currentDashSpeed * 0.5f;
                rb.AddForce(impulseForce, ForceMode2D.Impulse);
                
                // Debug logging for dash forces
                if (Time.frameCount % 10 == 0) // Log every 10 frames during dash
                {
                    Debug.Log($"Dash forces - Direction: {dashDirection}, Speed: {currentDashSpeed:F1}, Force: {dashForce.magnitude:F1}, Impulse: {impulseForce.magnitude:F1}, Velocity: {rb.linearVelocity.magnitude:F1}");
                }
            }
            else
            {
                // Direct velocity control for more reliable dashing
                rb.linearVelocity = dashDirection * currentDashSpeed;
                
                // Debug logging for dash velocity
                if (Time.frameCount % 10 == 0) // Log every 10 frames during dash
                {
                    Debug.Log($"Dash velocity - Direction: {dashDirection}, Speed: {currentDashSpeed:F1}, Velocity: {rb.linearVelocity.magnitude:F1}");
                }
            }
            
            // Check for enemies during dash (backup method)
            CheckForEnemiesDuringDash();
        }
        else
        {
            if (useForceBasedMovement)
            {
                // Force-based movement for softbody physics
                Vector2 targetVelocity = moveInput * speed;
                Vector2 currentVelocity = rb.linearVelocity;
                Vector2 velocityDifference = targetVelocity - currentVelocity;
                
                if (moveInput.magnitude > 0.1f)
                {
                    // Apply force to accelerate towards target velocity
                    Vector2 force = velocityDifference * acceleration * forceMultiplier;
                    force = Vector2.ClampMagnitude(force, maxForce);
                    rb.AddForce(force, ForceMode2D.Force);
                }
                else
                {
                    // Apply force to decelerate to zero
                    Vector2 force = -currentVelocity * deceleration * forceMultiplier;
                    force = Vector2.ClampMagnitude(force, maxForce);
                    rb.AddForce(force, ForceMode2D.Force);
                }
            }
            else
            {
                // Original velocity-based movement
                Vector2 targetVelocity = moveInput * speed;
                Vector2 currentVelocity = rb.linearVelocity;
                
                if (moveInput.magnitude > 0.1f)
                {
                    // Accelerate towards target velocity
                    rb.linearVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
                }
                else
                {
                    // Decelerate to zero when no input
                    rb.linearVelocity = Vector2.MoveTowards(currentVelocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
                }
            }
        }
    }

    void StartDash()
    {
        Debug.Log($"StartDash called! moveInput.magnitude: {moveInput.magnitude:F2}, lastMoveInput.magnitude: {lastMoveInput.magnitude:F2}, currentDashes: {currentDashes}");
        
        // Only dash if we have dashes available
        if (currentDashes > 0)
        {
            // Use current movement direction, or last movement direction if not currently moving
            Vector2 dashDir = moveInput.magnitude > 0.1f ? moveInput.normalized : lastMoveInput.normalized;
            
            // If we have no movement direction at all, dash forward (up)
            if (dashDir.magnitude < 0.1f)
            {
                dashDir = Vector2.up;
                Debug.Log("No movement direction found, dashing forward (up)");
            }
            
            isDashing = true;
            dashTimer = dashDuration;
            dashDirection = dashDir;
            dashCooldownTimer = dashCooldown;
            currentDashes--; // Consume a dash
            
            Debug.Log($"Player started dash in direction: {dashDirection}, remaining dashes: {currentDashes}");
        }
        else
        {
            Debug.Log("Cannot dash: No dashes available");
        }
    }

    void EndDash()
    {
        isDashing = false;
        dashTimer = 0f;
        // Stop the player when dash ends
        rb.linearVelocity = Vector2.zero;
    }
    
    // Handle collision with enemies during dash
    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDashing)
        {
            // Check if it's an enemy (has enemy_pathfinder component)
            if (other.GetComponent<enemy_pathfinder>() != null)
            {
                Debug.Log($"Player dashed through and killed enemy: {other.name}");
                Destroy(other.gameObject);
            }
        }
    }
    
    // Handle collision with enemies during dash (for non-trigger colliders)
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDashing)
        {
            // Check if it's an enemy (has enemy_pathfinder component)
            if (collision.gameObject.GetComponent<enemy_pathfinder>() != null)
            {
                Debug.Log($"Player dashed through and killed enemy: {collision.gameObject.name}");
                Destroy(collision.gameObject);
            }
        }
    }
    
    // Backup method to check for enemies during dash
    void CheckForEnemiesDuringDash()
    {
        // Find all enemies within dash kill radius
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, dashKillRadius);
        
        foreach (Collider2D enemy in enemies)
        {
            // Check if it's an enemy (has enemy_pathfinder component)
            if (enemy != null && enemy.GetComponent<enemy_pathfinder>() != null)
            {
                Debug.Log($"Player dashed through and killed enemy (backup method): {enemy.name}");
                Destroy(enemy.gameObject);
            }
        }
    }
    
    // Check if all enemies are killed and reward player
    void CheckForEnemyClear()
    {
        // Count remaining enemies
        int enemyCount = FindObjectsOfType<enemy_pathfinder>().Length;
        
        // If no enemies left and we have less than max dashes, refill dashes
        if (enemyCount == 0 && currentDashes < maxDashes)
        {
            ResetDashes();
            Debug.Log("All enemies cleared! Dashes refilled!");
        }
    }

    // Public method to check if player is dashing
    public bool IsDashing()
    {
        return isDashing;
    }

    // Public method to get remaining dash cooldown
    public float GetDashCooldownRemaining()
    {
        return Mathf.Max(0f, dashCooldownTimer);
    }
    
    // Public method to get total dash cooldown
    public float GetDashCooldownTotal()
    {
        return dashCooldown;
    }
    
    // Public method to get cooldown progress (0 = ready, 1 = just used)
    public float GetDashCooldownProgress()
    {
        return dashCooldown > 0 ? (dashCooldown - dashCooldownTimer) / dashCooldown : 0f;
    }
    
    // Public method to check if dash is ready
    public bool IsDashReady()
    {
        return dashCooldownTimer <= 0f && !isDashing && currentDashes > 0;
    }
    
    // Public method to get current dash count
    public int GetCurrentDashes()
    {
        return currentDashes;
    }
    
    // Public method to get max dashes
    public int GetMaxDashes()
    {
        return maxDashes;
    }
    
    // Public method to add dashes (for power-ups or respawn)
    public void AddDashes(int amount)
    {
        currentDashes = Mathf.Min(currentDashes + amount, maxDashes);
    }
    
    // Public method to reset dashes
    public void ResetDashes()
    {
        currentDashes = maxDashes;
        Debug.Log($"Dashes reset! Current dashes: {currentDashes}/{maxDashes}");
    }
    
    // Debug method to add dashes (for testing)
    [ContextMenu("Add Dash")]
    public void AddDash()
    {
        if (currentDashes < maxDashes)
        {
            currentDashes++;
            Debug.Log($"Dash added! Current dashes: {currentDashes}/{maxDashes}");
        }
    }
    
    // Debug method to reset dashes (for testing)
    [ContextMenu("Reset Dashes")]
    public void ResetDashesDebug()
    {
        ResetDashes();
    }
    
    // Visual feedback for dash cooldown using OnGUI
    void OnGUI()
    {
        // Position in top-left corner
        float x = 20f;
        float y = 20f;
        float width = 200f;
        float height = 20f;
        
        // Draw dash count
        GUI.color = Color.white;
        string dashText = $"DASHES: {currentDashes}/{maxDashes}";
        GUI.Label(new Rect(x, y, 200, 30), dashText);
        
        // Draw individual dash indicators
        for (int i = 0; i < maxDashes; i++)
        {
            Color dashColor = i < currentDashes ? Color.green : Color.gray;
            GUI.color = dashColor;
            GUI.DrawTexture(new Rect(x + (i * 25), y + 35, 20, 20), Texture2D.whiteTexture);
        }
        
        // Draw cooldown indicator if on cooldown or dashing
        if (dashCooldownTimer > 0f || isDashing)
        {
            float progress = GetDashCooldownProgress();
            bool isReady = IsDashReady();
            
            y += 30f; // Move down for cooldown bar
            
            // Background
            GUI.color = new Color(0, 0, 0, 0.5f);
            GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
            
            // Progress bar
            if (isDashing)
            {
                GUI.color = Color.yellow;
            }
            else if (isReady)
            {
                GUI.color = Color.green;
            }
            else
            {
                GUI.color = Color.red;
            }
            
            GUI.DrawTexture(new Rect(x, y, width * progress, height), Texture2D.whiteTexture);
            
            // Text
            GUI.color = Color.white;
            string text = isDashing ? "DASHING!" : 
                         isReady ? "DASH READY" : 
                         $"DASH: {dashCooldownTimer:F1}s";
            
            GUI.Label(new Rect(x, y + 25, 200, 30), text);
        }
        
        // Reset color
        GUI.color = Color.white;
    }
    
    // Debug visualization for dash kill radius
    void OnDrawGizmos()
    {
        if (isDashing)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, dashKillRadius);
        }
    }
}

