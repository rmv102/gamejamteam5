using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Spawning Settings")]
    [SerializeField] GameObject enemyPrefab; // Drag the Enemy prefab here
    [SerializeField] int maxEnemies = 5; // Maximum number of enemies in scene
    [SerializeField] float spawnRadius = 15f; // Minimum distance from player to spawn
    [SerializeField] float spawnInterval = 3f; // Base time between spawn attempts
    [SerializeField] float spawnIntervalRandomness = 1f; // Random variation in spawn timing
    [SerializeField] LayerMask veinLayer = ~0; // Layer containing vein objects
    
    [Header("Debug")]
    [SerializeField] bool showDebugInfo = true;
    [SerializeField] bool showSpawnRadius = true;
    
    private Transform player;
    private float lastSpawnTime;
    private float nextSpawnTime;
    private int currentEnemyCount = 0;
    
    void Start()
    {
        Debug.Log("EnemySpawner: Start() called!");
        
        // Find the player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            playerObj = GameObject.Find("Circle");
        }
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log("EnemySpawner: Found player at " + player.position);
        }
        else
        {
            Debug.LogError("EnemySpawner: No player found! Make sure player has 'Player' tag or is named 'Circle'");
        }
        
        lastSpawnTime = Time.time;
        SetNextSpawnTime();
        
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: No enemy prefab assigned! Please drag the Enemy prefab to the Enemy Prefab field.");
        }
        else
        {
            Debug.Log("EnemySpawner: Enemy prefab assigned: " + enemyPrefab.name);
        }
        
        Debug.Log("EnemySpawner: Initialization complete!");
    }
    
    void Update()
    {
        // Only spawn if we have an enemy prefab and player
        if (enemyPrefab == null || player == null) 
        {
            if (Time.frameCount % 300 == 0) // Log every 5 seconds at 60fps
            {
                Debug.LogWarning($"EnemySpawner: Cannot spawn - enemyPrefab: {(enemyPrefab != null ? "OK" : "NULL")}, player: {(player != null ? "OK" : "NULL")}");
            }
            return;
        }
        
        // Count current enemies
        currentEnemyCount = FindObjectsByType<enemy_pathfinder>(FindObjectsSortMode.None).Length;
        
        // Log status every 5 seconds
        if (showDebugInfo && Time.frameCount % 300 == 0)
        {
            Debug.Log($"EnemySpawner: Current enemies: {currentEnemyCount}/{maxEnemies}, Time until next spawn: {(nextSpawnTime - Time.time):F1}s");
        }
        
        // Try to spawn if under limit and enough time has passed
        if (currentEnemyCount < maxEnemies && Time.time >= nextSpawnTime)
        {
            Debug.Log("EnemySpawner: Attempting to spawn enemy...");
            Vector2 spawnPosition = FindSpawnPosition();
            if (spawnPosition != Vector2.zero)
            {
                SpawnEnemy(spawnPosition);
                lastSpawnTime = Time.time;
                SetNextSpawnTime();
            }
            else
            {
                Debug.LogWarning("EnemySpawner: Could not find valid spawn position!");
                // Try again sooner if we failed to find a position
                SetNextSpawnTime(0.5f); // Retry in 0.5 seconds
            }
        }
    }
    
    void SetNextSpawnTime(float overrideInterval = -1f)
    {
        float interval = overrideInterval > 0 ? overrideInterval : spawnInterval;
        float randomVariation = Random.Range(-spawnIntervalRandomness, spawnIntervalRandomness);
        nextSpawnTime = Time.time + interval + randomVariation;
        
        if (showDebugInfo)
        {
            Debug.Log($"EnemySpawner: Next spawn scheduled in {interval + randomVariation:F1} seconds (at {nextSpawnTime:F1})");
        }
    }
    
    Vector2 FindSpawnPosition()
    {
        // First try to spawn on veins
        Vector2 veinSpawnPosition = FindSpawnPositionOnVeins();
        if (veinSpawnPosition != Vector2.zero)
        {
            if (showDebugInfo)
            {
                Debug.Log($"EnemySpawner: Found spawn position on vein at {veinSpawnPosition}");
            }
            return veinSpawnPosition;
        }

        if (showDebugInfo)
        {
            Debug.LogWarning("EnemySpawner: No valid spawn position found on any vein! Trying emergency fallback...");
        }

        // Emergency fallback: spawn in a wider area around the player
        return FindEmergencySpawnPosition();
    }
    
    Vector2 FindEmergencySpawnPosition()
    {
        // Emergency fallback when vein spawning fails completely
        int attempts = 50;
        float emergencyRadius = spawnRadius * 1.5f; // Go further out
        
        if (showDebugInfo)
        {
            Debug.Log($"EnemySpawner: Using emergency fallback spawning with radius {emergencyRadius}");
        }
        
        for (int i = 0; i < attempts; i++)
        {
            // Generate random position around the player
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            float randomDistance = Random.Range(spawnRadius, emergencyRadius);
            Vector2 candidatePosition = (Vector2)player.position + randomDirection * randomDistance;
            
            // Check if position is clear of other enemies
            if (IsPositionClear(candidatePosition))
            {
                if (showDebugInfo)
                {
                    Debug.Log($"EnemySpawner: Found emergency spawn position at {candidatePosition} (distance: {Vector2.Distance(candidatePosition, player.position):F1})");
                }
                return candidatePosition;
            }
        }
        
        if (showDebugInfo)
        {
            Debug.LogWarning("EnemySpawner: Emergency fallback also failed!");
        }
        
        return Vector2.zero;
    }
    
    Vector2 FindSpawnPositionOnVeins()
    {
        // Try multiple methods to find vein objects
        GameObject[] veinObjects = FindVeinObjects();
        
        if (showDebugInfo)
        {
            Debug.Log($"EnemySpawner: Found {veinObjects.Length} vein objects using multiple detection methods");
            foreach (GameObject vein in veinObjects)
            {
                Collider2D collider = vein.GetComponent<Collider2D>();
                SpriteRenderer renderer = vein.GetComponent<SpriteRenderer>();
                Debug.Log($"  - {vein.name} at {vein.transform.position}, Has Collider: {collider != null}, Has Renderer: {renderer != null}");
            }
        }

        if (veinObjects.Length == 0)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("EnemySpawner: No vein objects found in scene! Trying emergency spawn...");
            }
            return Vector2.zero;
        }

        // Shuffle the vein array for more randomness
        for (int i = 0; i < veinObjects.Length; i++)
        {
            GameObject temp = veinObjects[i];
            int randomIndex = Random.Range(i, veinObjects.Length);
            veinObjects[i] = veinObjects[randomIndex];
            veinObjects[randomIndex] = temp;
        }

        // Try each vein in random order
        foreach (GameObject vein in veinObjects)
        {
            // Try to find a spawn position on this specific vein
            Vector2 spawnPos = FindSpawnPositionOnSpecificVein(vein);
            if (spawnPos != Vector2.zero)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"EnemySpawner: Successfully found spawn position on {vein.name} at {spawnPos}");
                }
                return spawnPos;
            }
        }

        if (showDebugInfo)
        {
            Debug.LogWarning("EnemySpawner: Could not find valid spawn position on any vein!");
        }

        return Vector2.zero;
    }
    
    GameObject[] FindVeinObjects()
    {
        List<GameObject> foundVeins = new List<GameObject>();
        
        // Method 1: Find by name containing "Vein"
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains("vein"))
            {
                foundVeins.Add(obj);
            }
        }
        
        // Method 2: Find by layer if veins are on a specific layer
        if (foundVeins.Count == 0)
        {
            Collider2D[] allColliders = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
            foreach (Collider2D collider in allColliders)
            {
                if (((1 << collider.gameObject.layer) & veinLayer) != 0)
                {
                    if (!foundVeins.Contains(collider.gameObject))
                    {
                        foundVeins.Add(collider.gameObject);
                    }
                }
            }
        }
        
        // Method 3: Find by red color (if veins are red)
        if (foundVeins.Count == 0)
        {
            SpriteRenderer[] allRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            foreach (SpriteRenderer renderer in allRenderers)
            {
                // Check if the sprite is red-ish
                if (renderer.color.r > 0.7f && renderer.color.g < 0.3f && renderer.color.b < 0.3f)
                {
                    if (!foundVeins.Contains(renderer.gameObject))
                    {
                        foundVeins.Add(renderer.gameObject);
                    }
                }
            }
        }
        
        return foundVeins.ToArray();
    }
    
    Vector2 FindSpawnPositionOnSpecificVein(GameObject vein)
    {
        // Get the vein's collider
        Collider2D veinCollider = vein.GetComponent<Collider2D>();
        SpriteRenderer veinRenderer = vein.GetComponent<SpriteRenderer>();
        
        if (showDebugInfo)
        {
            Debug.Log($"  - Testing {vein.name}: Collider={veinCollider != null}, Renderer={veinRenderer != null}");
        }
        
        // Method 1: Try collider-based spawning
        if (veinCollider != null)
        {
            Vector2 colliderSpawnPos = TryColliderBasedSpawning(vein, veinCollider);
            if (colliderSpawnPos != Vector2.zero)
            {
                return colliderSpawnPos;
            }
        }
        
        // Method 2: Try renderer-based spawning (for sprites without colliders)
        if (veinRenderer != null)
        {
            Vector2 rendererSpawnPos = TryRendererBasedSpawning(vein, veinRenderer);
            if (rendererSpawnPos != Vector2.zero)
            {
                return rendererSpawnPos;
            }
        }
        
        // Method 3: Fallback to transform position
        if (showDebugInfo)
        {
            Debug.Log($"  - All methods failed for {vein.name}, using transform position fallback");
        }
        return FindSpawnPositionNearVein(vein);
    }
    
    Vector2 TryColliderBasedSpawning(GameObject vein, Collider2D veinCollider)
    {
        Bounds veinBounds = veinCollider.bounds;
        float boundsSize = Mathf.Max(veinBounds.size.x, veinBounds.size.y);
        
        if (showDebugInfo)
        {
            Debug.Log($"    - Collider bounds: {veinBounds.min} to {veinBounds.max} (size: {boundsSize:F2})");
        }
        
        // If bounds are too small, skip collider method
        if (boundsSize < 0.1f)
        {
            if (showDebugInfo)
            {
                Debug.Log($"    - Bounds too small ({boundsSize:F2}), skipping collider method");
            }
            return Vector2.zero;
        }
        
        // Try multiple random points within the vein's bounds
        int attempts = 50; // Increased attempts
        
        for (int i = 0; i < attempts; i++)
        {
            // Generate random point within vein bounds
            Vector2 randomPoint = new Vector2(
                Random.Range(veinBounds.min.x, veinBounds.max.x),
                Random.Range(veinBounds.min.y, veinBounds.max.y)
            );
            
            // Check if point is far enough from player
            float distanceFromPlayer = Vector2.Distance(randomPoint, player.position);
            if (distanceFromPlayer < spawnRadius)
                continue;
                
            // Check if point is actually within the vein collider
            if (veinCollider.OverlapPoint(randomPoint))
            {
                // Check if position is clear of other enemies
                if (IsPositionClear(randomPoint))
                {
                    if (showDebugInfo)
                    {
                        Debug.Log($"    - Found collider-based spawn point on {vein.name} at {randomPoint}");
                    }
                    return randomPoint;
                }
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"    - Collider method failed after {attempts} attempts");
        }
        return Vector2.zero;
    }
    
    Vector2 TryRendererBasedSpawning(GameObject vein, SpriteRenderer veinRenderer)
    {
        Bounds rendererBounds = veinRenderer.bounds;
        float boundsSize = Mathf.Max(rendererBounds.size.x, rendererBounds.size.y);
        
        if (showDebugInfo)
        {
            Debug.Log($"    - Renderer bounds: {rendererBounds.min} to {rendererBounds.max} (size: {boundsSize:F2})");
        }
        
        // Try multiple random points within the renderer's bounds
        int attempts = 30;
        
        for (int i = 0; i < attempts; i++)
        {
            // Generate random point within renderer bounds
            Vector2 randomPoint = new Vector2(
                Random.Range(rendererBounds.min.x, rendererBounds.max.x),
                Random.Range(rendererBounds.min.y, rendererBounds.max.y)
            );
            
            // Check if point is far enough from player
            float distanceFromPlayer = Vector2.Distance(randomPoint, player.position);
            if (distanceFromPlayer < spawnRadius)
                continue;
                
            // For renderer-based, we assume any point in bounds is valid
            // Check if position is clear of other enemies
            if (IsPositionClear(randomPoint))
            {
                if (showDebugInfo)
                {
                    Debug.Log($"    - Found renderer-based spawn point on {vein.name} at {randomPoint}");
                }
                return randomPoint;
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"    - Renderer method failed after {attempts} attempts");
        }
        return Vector2.zero;
    }
    
    Vector2 FindSpawnPositionNearVein(GameObject vein)
    {
        // Fallback method: spawn in a small area around the vein's transform position
        Vector2 veinPosition = vein.transform.position;
        float searchRadius = 3f; // Search within 3 units of the vein
        int attempts = 20;
        
        if (showDebugInfo)
        {
            Debug.Log($"  - Using fallback spawning near {vein.name} at {veinPosition}");
        }
        
        for (int i = 0; i < attempts; i++)
        {
            // Generate random point around the vein
            Vector2 randomPoint = veinPosition + Random.insideUnitCircle * searchRadius;
            
            // Check if point is far enough from player
            float distanceFromPlayer = Vector2.Distance(randomPoint, player.position);
            if (distanceFromPlayer < spawnRadius)
                continue;
            
            // Check if position is clear of other enemies
            if (IsPositionClear(randomPoint))
            {
                if (showDebugInfo)
                {
                    Debug.Log($"  - Found fallback spawn point near {vein.name} at {randomPoint} (distance from player: {distanceFromPlayer:F1})");
                }
                return randomPoint;
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"  - Failed to find fallback spawn position near {vein.name}");
        }
        
        return Vector2.zero;
    }
    
    bool IsPositionOnVein(Vector2 position)
    {
        // Check if position is on a vein using multiple methods
        bool isOnVein = false;
        
        // Method 1: Raycast from position
        RaycastHit2D hit = Physics2D.Raycast(position, Vector2.zero, 0f, veinLayer);
        if (hit.collider != null)
        {
            isOnVein = true;
            if (showDebugInfo)
            {
                Debug.Log($"  - Vein check at {position}: TRUE (raycast hit: {hit.collider.name})");
            }
        }
        else
        {
            // Method 2: Check if position is within any vein collider
            Collider2D[] colliders = Physics2D.OverlapCircleAll(position, 0.1f, veinLayer);
            foreach (Collider2D col in colliders)
            {
                if (col.name.Contains("Vein"))
                {
                    isOnVein = true;
                    if (showDebugInfo)
                    {
                        Debug.Log($"  - Vein check at {position}: TRUE (overlap with: {col.name})");
                    }
                    break;
                }
            }
        }
        
        if (!isOnVein && showDebugInfo)
        {
            Debug.Log($"  - Vein check at {position}: FALSE (no vein found)");
        }
        
        return isOnVein;
    }
    
    bool IsPositionClear(Vector2 position)
    {
        // Check if position is clear of other enemies
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, 2f);
        foreach (Collider2D col in colliders)
        {
            if (col.GetComponent<enemy_pathfinder>() != null)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"  - Position {position} occupied by enemy {col.name}");
                }
                return false; // Position is occupied by another enemy
            }
        }
        return true;
    }
    
    void SpawnEnemy(Vector2 position)
    {
        // Instantiate new enemy
        GameObject newEnemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        
        // Ensure the enemy is properly activated and moving
        if (newEnemy != null)
        {
            newEnemy.SetActive(true);
            
            // Get the enemy pathfinder component and ensure it's working
            enemy_pathfinder pathfinder = newEnemy.GetComponent<enemy_pathfinder>();
            if (pathfinder != null)
            {
                // Force the enemy to start moving by calling its initialization
                if (showDebugInfo)
                {
                    Debug.Log($"EnemySpawner: Enemy spawned with pathfinder component at {position}");
                }
            }
            else
            {
                Debug.LogWarning($"EnemySpawner: Spawned enemy at {position} but no enemy_pathfinder component found!");
            }
            
            // Ensure rigidbody is properly configured
            Rigidbody2D enemyRb = newEnemy.GetComponent<Rigidbody2D>();
            if (enemyRb != null)
            {
                enemyRb.linearVelocity = Vector2.zero; // Reset velocity
                enemyRb.angularVelocity = 0f; // Reset angular velocity
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"EnemySpawner: Spawned new enemy at {position}. Total enemies: {currentEnemyCount + 1}");
        }
    }
    
    // Debug visualization
    void OnDrawGizmos()
    {
        if (player != null && showSpawnRadius)
        {
            // Draw spawn radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(player.position, spawnRadius);
            
            // Draw extended spawn radius
            Gizmos.color = Color.orange;
            Gizmos.DrawWireSphere(player.position, spawnRadius * 1.5f);
        }
    }
    
    // Public method to manually spawn an enemy (for testing)
    [ContextMenu("Spawn Enemy Now")]
    public void SpawnEnemyNow()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: No enemy prefab assigned!");
            return;
        }
        
        if (player == null)
        {
            Debug.LogError("EnemySpawner: No player found!");
            return;
        }
        
        Vector2 spawnPosition = FindSpawnPosition();
        if (spawnPosition != Vector2.zero)
        {
            SpawnEnemy(spawnPosition);
            Debug.Log($"EnemySpawner: Manually spawned enemy at {spawnPosition}");
        }
        else
        {
            Debug.LogWarning("EnemySpawner: Could not find valid spawn position for manual spawn!");
        }
    }
    
    // Debug method to check spawn system status
    [ContextMenu("Debug Spawn Status")]
    public void DebugSpawnStatus()
    {
        Debug.Log("=== Enemy Spawner Debug Status ===");
        Debug.Log($"Enemy Prefab: {(enemyPrefab != null ? enemyPrefab.name : "NULL")}");
        Debug.Log($"Player: {(player != null ? player.name + " at " + player.position : "NULL")}");
        Debug.Log($"Max Enemies: {maxEnemies}");
        Debug.Log($"Current Enemies: {FindObjectsByType<enemy_pathfinder>(FindObjectsSortMode.None).Length}");
        Debug.Log($"Spawn Radius: {spawnRadius}");
        Debug.Log($"Next Spawn Time: {nextSpawnTime:F1} (Current Time: {Time.time:F1})");
        Debug.Log($"Time Until Next Spawn: {(nextSpawnTime - Time.time):F1} seconds");
        
        // Test vein finding
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        GameObject[] veinObjects = allObjects.Where(go => go.name.Contains("Vein")).ToArray();
        Debug.Log($"Found {veinObjects.Length} vein objects:");
        foreach (GameObject vein in veinObjects)
        {
            Collider2D collider = vein.GetComponent<Collider2D>();
            Debug.Log($"  - {vein.name} at {vein.transform.position} (Has Collider: {collider != null})");
        }
        Debug.Log("=== End Debug Status ===");
    }
    
    // Force spawn an enemy for testing (ignores all conditions)
    [ContextMenu("Force Spawn Enemy (Test)")]
    public void ForceSpawnEnemy()
    {
        Debug.Log("EnemySpawner: Force spawning enemy for testing...");
        
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: Cannot force spawn - no enemy prefab assigned!");
            return;
        }
        
        // Spawn at a random position near the player (or at origin if no player)
        Vector2 spawnPos;
        if (player != null)
        {
            spawnPos = (Vector2)player.position + Random.insideUnitCircle * 10f;
        }
        else
        {
            spawnPos = Random.insideUnitCircle * 10f;
        }
        
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        Debug.Log($"EnemySpawner: Force spawned enemy at {spawnPos}");
        
        if (newEnemy == null)
        {
            Debug.LogError("EnemySpawner: Failed to instantiate enemy!");
        }
        else
        {
            Debug.Log($"EnemySpawner: Successfully created enemy: {newEnemy.name}");
        }
    }
}

