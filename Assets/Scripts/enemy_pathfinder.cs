using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class enemy_pathfinder : MonoBehaviour
{
    [Header("Pathfinding Settings")]
    [SerializeField] float moveSpeed = 1.5f;
    [SerializeField] float detectionRange = 10f;
    [SerializeField] float stoppingDistance = 1f;
    [SerializeField] float pathUpdateInterval = 0.2f;
    
    [Header("Movement Settings")]
    [SerializeField] float acceleration = 8f;
    [SerializeField] float deceleration = 12f;
    [SerializeField] LayerMask obstacleLayer = ~0; // Collide with everything by default
    
    [Header("Stuck Detection")]
    [SerializeField] float stuckThreshold = 0.1f;
    [SerializeField] float stuckTimeThreshold = 1f;
    [SerializeField] float unstuckForce = 3f;
    
    [Header("Border Avoidance")]
    [SerializeField] float borderAvoidanceDistance = 1.5f;
    [SerializeField] float borderPushForce = 5f;
    [SerializeField] LayerMask borderLayer = ~0;
    
    private Transform player;
    private Rigidbody2D rb;
    private Vector2 targetPosition;
    private Vector2 currentVelocity;
    private float lastPathUpdate;
    private bool isFollowingPlayer = false;
    
    // Stuck detection variables
    private Vector2 lastPosition;
    private float stuckTimer;
    private Vector2 lastVelocity;
    private int stuckDirection = 1;
    
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
        
        // Initialize target position and stuck detection
        targetPosition = transform.position;
        lastPosition = transform.position;
        lastVelocity = Vector2.zero;
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
            // Update path periodically
            if (Time.time - lastPathUpdate >= pathUpdateInterval)
            {
                UpdatePath();
                lastPathUpdate = Time.time;
            }
            
            // Check for stuck condition
            CheckStuckCondition();
            
            // Check for border avoidance
            CheckBorderAvoidance();
            
            // Move towards target
            MoveTowardsTarget();
        }
        else
        {
            // Stop moving if player is too far
            StopMoving();
        }
    }
    
    void CheckStuckCondition()
    {
        float distanceMoved = Vector2.Distance(transform.position, lastPosition);
        float velocityMagnitude = currentVelocity.magnitude;
        
        // Check if we're stuck (not moving much and low velocity)
        if (distanceMoved < stuckThreshold && velocityMagnitude < stuckThreshold)
        {
            stuckTimer += Time.deltaTime;
            
            if (stuckTimer >= stuckTimeThreshold)
            {
                // We're stuck! Try to get unstuck
                GetUnstuck();
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
        
        // Update tracking variables
        lastPosition = transform.position;
        lastVelocity = currentVelocity;
    }
    
    void CheckBorderAvoidance()
    {
        // Check for nearby borders in multiple directions
        Vector2[] checkDirections = {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right,
            new Vector2(1, 1).normalized,
            new Vector2(-1, 1).normalized,
            new Vector2(1, -1).normalized,
            new Vector2(-1, -1).normalized
        };
        
        Vector2 avoidanceForce = Vector2.zero;
        int borderHits = 0;
        
        foreach (Vector2 dir in checkDirections)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, borderAvoidanceDistance, borderLayer);
            
            if (hit.collider != null)
            {
                // Calculate distance to border
                float distanceToBorder = hit.distance;
                float avoidanceStrength = 1f - (distanceToBorder / borderAvoidanceDistance);
                
                // Create force away from border
                Vector2 awayFromBorder = -dir;
                avoidanceForce += awayFromBorder * avoidanceStrength;
                borderHits++;
            }
        }
        
        // Apply border avoidance force
        if (borderHits > 0)
        {
            avoidanceForce = avoidanceForce.normalized * borderPushForce;
            rb.AddForce(avoidanceForce, ForceMode2D.Force);
        }
    }
    
    void GetUnstuck()
    {
        // Try to move in a random direction to get unstuck
        Vector2 randomDirection = new Vector2(
            Random.Range(-1f, 1f), 
            Random.Range(-1f, 1f)
        ).normalized;
        
        // Apply a force in the random direction
        rb.AddForce(randomDirection * unstuckForce, ForceMode2D.Impulse);
        
        // Also try to move away from the current position
        Vector2 awayFromCurrent = (transform.position - (Vector3)lastPosition).normalized;
        if (awayFromCurrent.magnitude > 0.1f)
        {
            rb.AddForce(awayFromCurrent * unstuckForce * 0.5f, ForceMode2D.Impulse);
        }
        
        Debug.Log($"Enemy {gameObject.name} is stuck! Applying unstuck force.");
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
        
        // Use raycast to check for obstacles in the direct path
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleLayer);
        
        if (hit.collider != null)
        {
            // There's an obstacle, try to find a way around it
            // Test directions in order of preference (closest to player direction first)
            Vector2[] directions = {
                directionToPlayer, // Always try direct path first
                new Vector2(directionToPlayer.x, 0).normalized, // Horizontal component
                new Vector2(0, directionToPlayer.y).normalized, // Vertical component
                new Vector2(directionToPlayer.y, directionToPlayer.x).normalized, // Perpendicular 1
                new Vector2(-directionToPlayer.y, directionToPlayer.x).normalized, // Perpendicular 2
                Vector2.up,
                Vector2.down,
                Vector2.left,
                Vector2.right
            };
            
            Vector2 bestDirection = directionToPlayer;
            float bestScore = -1f;
            Vector2 bestTarget = player.position;
            
            foreach (Vector2 dir in directions)
            {
                if (dir.magnitude < 0.1f) continue; // Skip invalid directions
                
                RaycastHit2D testHit = Physics2D.Raycast(transform.position, dir, detectionRange, obstacleLayer);
                float testDistance = testHit.collider != null ? testHit.distance : detectionRange;
                
                // Check for nearby borders in this direction
                RaycastHit2D borderHit = Physics2D.Raycast(transform.position, dir, borderAvoidanceDistance, borderLayer);
                float borderDistance = borderHit.collider != null ? borderHit.distance : borderAvoidanceDistance;
                
                // Calculate score: prioritize directions toward player AND clear paths AND away from borders
                float directionScore = Vector2.Dot(dir, directionToPlayer);
                float clearPathScore = testDistance / detectionRange; // How clear the path is
                float borderAvoidanceScore = borderDistance / borderAvoidanceDistance; // How far from borders
                
                // Penalize directions that are too close to borders
                if (borderDistance < borderAvoidanceDistance * 0.5f)
                {
                    borderAvoidanceScore *= 0.3f; // Heavy penalty for being too close to borders
                }
                
                float totalScore = directionScore * 0.5f + clearPathScore * 0.3f + borderAvoidanceScore * 0.2f;
                
                if (totalScore > bestScore && testDistance > 0.5f && borderDistance > 0.3f)
                {
                    bestScore = totalScore;
                    bestDirection = dir;
                    bestTarget = (Vector2)transform.position + dir * Mathf.Min(testDistance * 0.8f, distanceToPlayer);
                }
            }
            
            // Use the best direction found
            targetPosition = bestTarget;
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
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        // Don't move if we're close enough to the player
        if (distanceToPlayer <= stoppingDistance)
        {
            StopMoving();
            return;
        }
        
        // Always prioritize moving directly toward player if possible
        Vector2 directionToPlayer = (player.position - transform.position).normalized;
        Vector2 directionToTarget = (targetPosition - (Vector2)transform.position).normalized;
        float distanceToTarget = Vector2.Distance(transform.position, targetPosition);
        
        Vector2 finalDirection;
        
        // If we're very close to our pathfinding target, move directly toward player
        if (distanceToTarget < 0.5f)
        {
            finalDirection = directionToPlayer;
        }
        else
        {
            // Use pathfinding direction, but ensure it's generally toward the player
            float dotProduct = Vector2.Dot(directionToTarget, directionToPlayer);
            
            // If the pathfinding direction is too far from player direction, use player direction
            if (dotProduct < 0.3f) // If angle is more than ~72 degrees from player
            {
                finalDirection = directionToPlayer;
            }
            else
            {
                finalDirection = directionToTarget;
            }
        }
        
        // Calculate target velocity
        Vector2 targetVelocity = finalDirection * moveSpeed;
        
        // Smoothly accelerate towards target velocity
        currentVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.deltaTime);
        
        // Apply velocity
        rb.linearVelocity = currentVelocity;
        
        // Debug output
        Debug.Log($"Enemy {gameObject.name}: Player dist: {distanceToPlayer:F2}, Target dist: {distanceToTarget:F2}, Direction: {finalDirection}, Velocity: {currentVelocity.magnitude:F2}");
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
            // Draw line to current target (pathfinding target)
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetPosition);
            
            // Draw line to player
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, player.position);
            
            // Draw stuck indicator
            if (stuckTimer > 0)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, 0.5f);
            }
            
            // Draw border avoidance range
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, borderAvoidanceDistance);
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
    
    // Handle collision with borders
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if we collided with a border
        if (((1 << collision.gameObject.layer) & borderLayer) != 0)
        {
            // Get the collision normal (direction away from the border)
            Vector2 collisionNormal = collision.contacts[0].normal;
            
            // Apply a push force away from the border
            Vector2 pushForce = collisionNormal * borderPushForce;
            rb.AddForce(pushForce, ForceMode2D.Impulse);
            
            Debug.Log($"Enemy {gameObject.name} hit border! Applying push force: {pushForce}");
        }
    }
    
    void OnCollisionStay2D(Collision2D collision)
    {
        // If we're still colliding with a border, keep pushing away
        if (((1 << collision.gameObject.layer) & borderLayer) != 0)
        {
            Vector2 collisionNormal = collision.contacts[0].normal;
            Vector2 pushForce = collisionNormal * borderPushForce * 0.5f; // Smaller force for continuous collision
            rb.AddForce(pushForce, ForceMode2D.Force);
        }
    }
}
