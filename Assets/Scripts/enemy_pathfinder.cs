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
    
    [Header("Target Priority")]
    [SerializeField] float particleDetectionRange = 15f; // Increased range for better particle detection
    [SerializeField] string infectedParticleTag = "InfectedParticle";
    [SerializeField] float particlePriorityMultiplier = 3f; // How much more important particles are
    [SerializeField] float particleStoppingDistance = 0.1f; // Very small stopping distance for particles
    
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
    
    // Target tracking
    private Transform currentTarget;
    private bool isTargetingParticle = false;
    
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
        
        // Find the best target (player or infected particle)
        FindBestTarget();
        
        // If there are particles in the scene, be more aggressive about finding them
        if (AreThereAnyParticles() && currentTarget == null)
        {
            // Increase detection range temporarily when particles exist but none are in range
            float originalRange = particleDetectionRange;
            particleDetectionRange = originalRange * 1.5f; // 50% increase
            
            // Try to find particles again with extended range
            FindBestTarget();
            
            // Restore original range
            particleDetectionRange = originalRange;
        }
        
        if (isFollowingPlayer)
        {
            // Update path more frequently when targeting particles
            float updateInterval = isTargetingParticle ? pathUpdateInterval * 0.5f : pathUpdateInterval;
            
            if (Time.time - lastPathUpdate >= updateInterval)
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
            // If there are particles but we're not following, try to find them
            if (AreThereAnyParticles())
            {
                // Move randomly to search for particles
                SearchForParticles();
            }
            else
            {
                // Stop moving if no targets are in range
                StopMoving();
            }
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
    
    void FindBestTarget()
    {
        if (player == null) return;
        
        Transform bestTarget = null;
        float bestScore = float.MaxValue;
        bool targetingParticle = false;
        
        // FIRST PRIORITY: Look for infected particles within detection range
        GameObject[] particles = GameObject.FindGameObjectsWithTag(infectedParticleTag);
        
        foreach (GameObject particle in particles)
        {
            if (particle == null) continue; // Skip destroyed particles
            
            float distanceToParticle = Vector2.Distance(transform.position, particle.transform.position);
            
            // Check if particle is within detection range
            if (distanceToParticle <= particleDetectionRange)
            {
                // Calculate priority score (lower is better)
                // Particles get massive priority boost - even far particles are prioritized
                float particleScore = distanceToParticle / particlePriorityMultiplier;
                
                if (particleScore < bestScore)
                {
                    bestTarget = particle.transform;
                    bestScore = particleScore;
                    targetingParticle = true;
                }
            }
        }
        
        // If we found a particle, ALWAYS prioritize it over player
        if (bestTarget != null && targetingParticle)
        {
            currentTarget = bestTarget;
            isTargetingParticle = true;
            isFollowingPlayer = true;
            return;
        }
        
        // SECOND PRIORITY: Only target player if NO particles are found at all
        if (bestTarget == null)
        {
            float playerDistance = Vector2.Distance(transform.position, player.position);
            if (playerDistance <= detectionRange)
            {
                bestTarget = player;
                bestScore = playerDistance;
                targetingParticle = false;
            }
        }
        
        // Update current target
        currentTarget = bestTarget;
        isTargetingParticle = targetingParticle;
        
        // Update following status - only follow if we have a target
        isFollowingPlayer = currentTarget != null;
    }
    
    // Method to check if there are any particles in the scene at all
    bool AreThereAnyParticles()
    {
        GameObject[] particles = GameObject.FindGameObjectsWithTag(infectedParticleTag);
        return particles.Length > 0;
    }
    
    void SearchForParticles()
    {
        // Move in a random direction to search for particles
        Vector2 randomDirection = new Vector2(
            Random.Range(-1f, 1f), 
            Random.Range(-1f, 1f)
        ).normalized;
        
        // Apply search movement
        Vector2 searchVelocity = randomDirection * moveSpeed * 0.5f; // Slower search speed
        currentVelocity = Vector2.MoveTowards(currentVelocity, searchVelocity, acceleration * Time.deltaTime);
        rb.linearVelocity = currentVelocity;
        
        Debug.Log($"Enemy {gameObject.name} searching for particles...");
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
        if (currentTarget == null) return;
        
        Vector2 directionToTarget = (currentTarget.position - transform.position).normalized;
        float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);
        
        // Use different stopping distances for particles vs player
        float effectiveStoppingDistance = isTargetingParticle ? particleStoppingDistance : stoppingDistance;
        
        // If target is close enough, move directly towards them
        if (distanceToTarget <= effectiveStoppingDistance)
        {
            targetPosition = currentTarget.position;
            return;
        }
        
        // Use raycast to check for obstacles in the direct path
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToTarget, distanceToTarget, obstacleLayer);
        
        if (hit.collider != null)
        {
            // There's an obstacle, try to find a way around it
            // Test directions in order of preference (closest to target direction first)
            Vector2[] directions = {
                directionToTarget, // Always try direct path first
                new Vector2(directionToTarget.x, 0).normalized, // Horizontal component
                new Vector2(0, directionToTarget.y).normalized, // Vertical component
                new Vector2(directionToTarget.y, directionToTarget.x).normalized, // Perpendicular 1
                new Vector2(-directionToTarget.y, directionToTarget.x).normalized, // Perpendicular 2
                Vector2.up,
                Vector2.down,
                Vector2.left,
                Vector2.right
            };
            
            Vector2 bestDirection = directionToTarget;
            float bestScore = -1f;
            Vector2 bestTarget = currentTarget.position;
            
            foreach (Vector2 dir in directions)
            {
                if (dir.magnitude < 0.1f) continue; // Skip invalid directions
                
                RaycastHit2D testHit = Physics2D.Raycast(transform.position, dir, detectionRange, obstacleLayer);
                float testDistance = testHit.collider != null ? testHit.distance : detectionRange;
                
                // Check for nearby borders in this direction
                RaycastHit2D borderHit = Physics2D.Raycast(transform.position, dir, borderAvoidanceDistance, borderLayer);
                float borderDistance = borderHit.collider != null ? borderHit.distance : borderAvoidanceDistance;
                
                // Calculate score: prioritize directions toward target AND clear paths AND away from borders
                float directionScore = Vector2.Dot(dir, directionToTarget);
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
                    bestTarget = (Vector2)transform.position + dir * Mathf.Min(testDistance * 0.8f, distanceToTarget);
                }
            }
            
            // Use the best direction found
            targetPosition = bestTarget;
        }
        else
        {
            // No obstacles, move directly towards target
            targetPosition = currentTarget.position;
        }
    }
    
    void MoveTowardsTarget()
    {
        if (currentTarget == null) return;
        
        float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);
        
        // Use different stopping distances for particles vs player
        float effectiveStoppingDistance = isTargetingParticle ? particleStoppingDistance : stoppingDistance;
        
        // Don't move if we're close enough to the target
        if (distanceToTarget <= effectiveStoppingDistance)
        {
            // For particles, don't stop - keep moving to ensure collision
            if (isTargetingParticle)
            {
                // Move directly toward the particle to ensure collision
                Vector2 particleDirection = (currentTarget.position - transform.position).normalized;
                Vector2 particleVelocity = particleDirection * moveSpeed;
                currentVelocity = Vector2.MoveTowards(currentVelocity, particleVelocity, acceleration * Time.deltaTime);
                rb.linearVelocity = currentVelocity;
                return;
            }
            else
            {
                StopMoving();
                return;
            }
        }
        
        // Calculate direction to current target
        Vector2 directionToTarget = (currentTarget.position - transform.position).normalized;
        Vector2 directionToPathfindingTarget = (targetPosition - (Vector2)transform.position).normalized;
        float distanceToPathfindingTarget = Vector2.Distance(transform.position, targetPosition);
        
        Vector2 finalDirection;
        
        // If we're very close to our pathfinding target, move directly toward current target
        if (distanceToPathfindingTarget < 0.5f)
        {
            finalDirection = directionToTarget;
        }
        else
        {
            // Use pathfinding direction, but ensure it's generally toward the current target
            float dotProduct = Vector2.Dot(directionToPathfindingTarget, directionToTarget);
            
            // If the pathfinding direction is too far from target direction, use target direction
            if (dotProduct < 0.3f) // If angle is more than ~72 degrees from target
            {
                finalDirection = directionToTarget;
            }
            else
            {
                finalDirection = directionToPathfindingTarget;
            }
        }
        
        // Calculate target velocity
        Vector2 targetVelocity = finalDirection * moveSpeed;
        
        // Smoothly accelerate towards target velocity
        currentVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.deltaTime);
        
        // Apply velocity
        rb.linearVelocity = currentVelocity;
        
        // Debug output
        string targetType = isTargetingParticle ? "PARTICLE (PRIORITY)" : "Player";
        string priority = isTargetingParticle ? "HIGH" : "LOW";
        float effectiveStopDist = isTargetingParticle ? particleStoppingDistance : stoppingDistance;
        Debug.Log($"Enemy {gameObject.name}: Target: {targetType}, Priority: {priority}, Dist: {distanceToTarget:F2}, StopDist: {effectiveStopDist:F2}, Direction: {finalDirection}, Velocity: {currentVelocity.magnitude:F2}");
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
        if (isFollowingPlayer && currentTarget != null)
        {
            // Draw line to current target (pathfinding target)
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetPosition);
            
            // Draw line to current target (player or particle)
            if (isTargetingParticle)
            {
                Gizmos.color = Color.cyan; // Cyan for particles
            }
            else
            {
                Gizmos.color = Color.green; // Green for player
            }
            Gizmos.DrawLine(transform.position, currentTarget.position);
            
            // Draw stuck indicator
            if (stuckTimer > 0)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, 0.5f);
            }
            
            // Draw border avoidance range
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, borderAvoidanceDistance);
            
            // Draw particle detection range
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, particleDetectionRange);
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
    
    // Handle trigger collision with infected particles
    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if we collided with an infected particle
        if (other.CompareTag(infectedParticleTag))
        {
            Debug.Log($"Enemy {gameObject.name} COLLIDED with particle! Distance: {Vector2.Distance(transform.position, other.transform.position):F3}");
            
            // Consume the particle (destroy it)
            Destroy(other.gameObject);
            
            Debug.Log($"Enemy {gameObject.name} CONSUMED infected particle! Searching for more...");
            
            // Immediately search for the next particle
            FindBestTarget();
            
            // If we were targeting this particle, find a new target
            if (isTargetingParticle && currentTarget == other.transform)
            {
                FindBestTarget();
            }
            
            // Apply a small speed boost after consuming a particle
            currentVelocity *= 1.2f;
            rb.linearVelocity = currentVelocity;
        }
    }
}
