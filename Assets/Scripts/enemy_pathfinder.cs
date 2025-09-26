using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class enemy_pathfinder : MonoBehaviour
{
    [Header("Pathfinding Settings")]
    [SerializeField] float moveSpeed = 1.5f;
    [SerializeField] float detectionRange = 10f;
    [SerializeField] float stoppingDistance = 1f;
    [SerializeField] float pathUpdateInterval = 0.5f;
    
    [Header("Movement Settings")]
    [SerializeField] float acceleration = 4f;
    [SerializeField] float deceleration = 8f;
    [SerializeField] LayerMask obstacleLayer = ~0; // Collide with everything by default
    
    private Transform player;
    private Rigidbody2D rb;
    private Vector2 targetPosition;
    private Vector2 currentVelocity;
    private float lastPathUpdate;
    private bool isFollowingPlayer = false;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Configure Rigidbody2D for smooth enemy movement
        if (rb != null)
        {
            rb.gravityScale = 0f; // No gravity for 2D top-down movement
            rb.linearDamping = 0f; // No air resistance
            rb.angularDamping = 0f; // No rotation damping
            rb.freezeRotation = true; // Prevent rotation
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // Smooth interpolation
        }
        
        // Find the player (assuming it has a tag "Player" or find by name)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            // If no player tag, try to find by name
            playerObj = GameObject.Find("Circle");
        }
        
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("Enemy pathfinder: No player found! Make sure player has 'Player' tag or is named 'Circle'");
        }
        
        // Initialize target position
        targetPosition = transform.position;
    }
    
    void Update()
    {
        if (player == null) 
        {
            Debug.LogWarning("Enemy pathfinder: Player not found!");
            return;
        }
        
        // Check if player is within detection range
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        isFollowingPlayer = distanceToPlayer <= detectionRange;
        
        if (isFollowingPlayer)
        {
            // Move directly towards the player
            MoveTowardsTarget();
        }
        else
        {
            // Stop moving if player is too far
            StopMoving();
        }
    }
    
    void UpdatePath()
    {
        if (player == null) return;
        
        Vector2 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        // If player is close enough, move directly towards them
        if (distanceToPlayer <= stoppingDistance)
        {
            targetPosition = player.position;
            return;
        }
        
        // Check if we're currently stuck (not moving much)
        bool isStuck = currentVelocity.magnitude < 0.1f;
        
        // Use raycast to check for obstacles in the direct path
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleLayer);
        
        if (hit.collider != null || isStuck)
        {
            // There's an obstacle or we're stuck, try to find a way around it
            Vector2[] directions = {
                Vector2.up,
                Vector2.down,
                Vector2.left,
                Vector2.right,
                new Vector2(1, 1).normalized,
                new Vector2(-1, 1).normalized,
                new Vector2(1, -1).normalized,
                new Vector2(-1, -1).normalized,
                // Add more directions for better pathfinding
                new Vector2(0.7f, 0.7f).normalized,
                new Vector2(-0.7f, 0.7f).normalized,
                new Vector2(0.7f, -0.7f).normalized,
                new Vector2(-0.7f, -0.7f).normalized
            };
            
            Vector2 bestDirection = directionToPlayer;
            float bestDistance = 0f;
            Vector2 bestTarget = player.position;
            
            foreach (Vector2 dir in directions)
            {
                RaycastHit2D testHit = Physics2D.Raycast(transform.position, dir, detectionRange, obstacleLayer);
                float testDistance = testHit.collider != null ? testHit.distance : detectionRange;
                
                // Prefer directions that are closer to the player
                float directionScore = Vector2.Dot(dir, directionToPlayer);
                
                if (testDistance > bestDistance || (testDistance > 1f && directionScore > 0.5f))
                {
                    bestDistance = testDistance;
                    bestDirection = dir;
                    bestTarget = (Vector2)transform.position + dir * Mathf.Min(testDistance * 0.9f, distanceToPlayer);
                }
            }
            
            // If we found a good direction, use it
            if (bestDistance > 0.5f)
            {
                targetPosition = bestTarget;
            }
            else
            {
                // If all directions are blocked, try to move away from the obstacle
                Vector2 awayFromObstacle = -directionToPlayer;
                targetPosition = (Vector2)transform.position + awayFromObstacle * 2f;
            }
        }
        else
        {
            // No obstacles, move directly towards player
            targetPosition = player.position;
        }
    }
    
    void MoveTowardsTarget()
    {
        if (player == null) return;
        
        // Move directly towards the player - simple and reliable
        Vector2 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        // Don't move if we're close enough to the player
        if (distanceToPlayer <= stoppingDistance)
        {
            StopMoving();
            return;
        }
        
        // Calculate target velocity directly towards player
        Vector2 targetVelocity = directionToPlayer * moveSpeed;
        
        // Smoothly accelerate towards target velocity
        currentVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.deltaTime);
        
        // Apply velocity
        rb.linearVelocity = currentVelocity;
        
        // Debug output
        Debug.Log($"Enemy moving towards player: Distance: {distanceToPlayer:F2}, Velocity: {currentVelocity.magnitude:F2}");
    }
    
    void StopMoving()
    {
        // Smoothly decelerate to zero
        currentVelocity = Vector2.MoveTowards(currentVelocity, Vector2.zero, deceleration * Time.deltaTime);
        rb.linearVelocity = currentVelocity;
    }
    
    void OnDrawGizmos()
    {
        // Draw detection range using multiple lines to create a circle
        Gizmos.color = Color.red;
        int segments = 32;
        float angleStep = 360f / segments;
        Vector3 prevPoint = transform.position + new Vector3(detectionRange, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = transform.position + new Vector3(Mathf.Cos(angle) * detectionRange, Mathf.Sin(angle) * detectionRange, 0);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
        
        // Draw path to target
        if (isFollowingPlayer && player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetPosition);
            
            // Draw line to player
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
    
    // Public method to check if enemy is following player
    public bool IsFollowingPlayer()
    {
        return isFollowingPlayer;
    }
    
    // Public method to get distance to player
    public float GetDistanceToPlayer()
    {
        if (player == null) return float.MaxValue;
        return Vector2.Distance(transform.position, player.position);
    }
}
