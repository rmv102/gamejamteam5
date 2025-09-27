using UnityEngine;
using System.Linq;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Spawning Settings")]
    [SerializeField] GameObject enemyPrefab; // Drag the Enemy prefab here
    [SerializeField] int maxEnemies = 5; // Maximum number of enemies in scene
    [SerializeField] float spawnRadius = 15f; // Minimum distance from player to spawn
    [SerializeField] float spawnInterval = 3f; // Time between spawn attempts
    [SerializeField] LayerMask veinLayer = ~0; // Layer containing vein objects
    
    [Header("Debug")]
    [SerializeField] bool showDebugInfo = true;
    [SerializeField] bool showSpawnRadius = true;
    
    private Transform player;
    private float lastSpawnTime;
    private int currentEnemyCount = 0;
    
    void Start()
    {
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
        
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: No enemy prefab assigned! Please drag the Enemy prefab to the Enemy Prefab field.");
        }
    }
    
    void Update()
    {
        // Only spawn if we have an enemy prefab and player
        if (enemyPrefab == null || player == null) return;
        
        // Count current enemies
        currentEnemyCount = FindObjectsOfType<enemy_pathfinder>().Length;
        
        if (showDebugInfo)
        {
            Debug.Log($"EnemySpawner: Current enemies: {currentEnemyCount}/{maxEnemies}, Time since last spawn: {Time.time - lastSpawnTime:F1}s");
        }
        
        // Try to spawn if under limit and enough time has passed
        if (currentEnemyCount < maxEnemies && Time.time - lastSpawnTime >= spawnInterval)
        {
            Vector2 spawnPosition = FindSpawnPosition();
            if (spawnPosition != Vector2.zero)
            {
                SpawnEnemy(spawnPosition);
                lastSpawnTime = Time.time;
            }
            else
            {
                Debug.LogWarning("EnemySpawner: Could not find valid spawn position!");
            }
        }
    }
    
    Vector2 FindSpawnPosition()
    {
        // First, try to find valid spawn points on actual veins
        Vector2 veinSpawnPosition = FindSpawnPositionOnVeins();
        if (veinSpawnPosition != Vector2.zero)
        {
            return veinSpawnPosition;
        }
        
        // Fallback: try random positions around player
        int maxAttempts = 20;
        for (int i = 0; i < maxAttempts; i++)
        {
            // Generate random position around the player
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            float randomDistance = Random.Range(spawnRadius, spawnRadius * 1.5f);
            Vector2 candidatePosition = (Vector2)player.position + randomDirection * randomDistance;
            
            if (showDebugInfo)
            {
                Debug.Log($"EnemySpawner: Fallback attempt {i + 1}: Trying position {candidatePosition}, Distance from player: {Vector2.Distance(candidatePosition, player.position):F1}");
            }
            
            // Check if position is far enough from player
            if (Vector2.Distance(candidatePosition, player.position) < spawnRadius)
            {
                if (showDebugInfo) Debug.Log("  - Too close to player, skipping");
                continue;
            }
                
            // Check if position is on a vein (natural area)
            if (IsPositionOnVein(candidatePosition))
            {
                if (showDebugInfo) Debug.Log("  - Position is on vein");
                // Check if position is clear of other enemies
                if (IsPositionClear(candidatePosition))
                {
                    if (showDebugInfo) Debug.Log("  - Position is clear, using this spawn point!");
                    return candidatePosition;
                }
                else
                {
                    if (showDebugInfo) Debug.Log("  - Position occupied by another enemy");
                }
            }
            else
            {
                if (showDebugInfo) Debug.Log("  - Position not on vein");
            }
        }
        
        return Vector2.zero; // No valid spawn position found
    }
    
    Vector2 FindSpawnPositionOnVeins()
    {
        // Find all vein objects in the scene
        GameObject[] veinObjects = GameObject.FindGameObjectsWithTag("Untagged"); // Veins might not have tags
        if (veinObjects.Length == 0)
        {
            // Try finding by name - find all GameObjects and filter by name
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            veinObjects = allObjects.Where(go => go.name.Contains("Vein")).ToArray();
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"EnemySpawner: Found {veinObjects.Length} potential vein objects");
        }
        
        foreach (GameObject vein in veinObjects)
        {
            if (vein.name.Contains("Vein"))
            {
                // Try to find a spawn position on this specific vein
                Vector2 spawnPos = FindSpawnPositionOnSpecificVein(vein);
                if (spawnPos != Vector2.zero)
                {
                    if (showDebugInfo)
                    {
                        Debug.Log($"EnemySpawner: Found spawn position on {vein.name} at {spawnPos}");
                    }
                    return spawnPos;
                }
            }
        }
        
        return Vector2.zero;
    }
    
    Vector2 FindSpawnPositionOnSpecificVein(GameObject vein)
    {
        // Get the vein's collider
        Collider2D veinCollider = vein.GetComponent<Collider2D>();
        if (veinCollider == null) return Vector2.zero;
        
        // Try multiple random points within the vein's bounds
        Bounds veinBounds = veinCollider.bounds;
        int attempts = 10;
        
        for (int i = 0; i < attempts; i++)
        {
            // Generate random point within vein bounds
            Vector2 randomPoint = new Vector2(
                Random.Range(veinBounds.min.x, veinBounds.max.x),
                Random.Range(veinBounds.min.y, veinBounds.max.y)
            );
            
            // Check if point is far enough from player
            if (Vector2.Distance(randomPoint, player.position) < spawnRadius)
                continue;
                
            // Check if point is actually within the vein collider
            if (veinCollider.OverlapPoint(randomPoint))
            {
                // Check if position is clear of other enemies
                if (IsPositionClear(randomPoint))
                {
                    if (showDebugInfo)
                    {
                        Debug.Log($"  - Found valid spawn point on {vein.name} at {randomPoint}");
                    }
                    return randomPoint;
                }
            }
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
                return false; // Position is occupied by another enemy
            }
        }
        return true;
    }
    
    void SpawnEnemy(Vector2 position)
    {
        // Instantiate new enemy
        GameObject newEnemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        Debug.Log($"EnemySpawner: Spawned new enemy at {position}. Total enemies: {currentEnemyCount + 1}");
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
            Debug.LogError("No enemy prefab assigned!");
            return;
        }
        
        Vector2 spawnPosition = FindSpawnPosition();
        if (spawnPosition != Vector2.zero)
        {
            SpawnEnemy(spawnPosition);
        }
        else
        {
            Debug.LogWarning("Could not find valid spawn position for manual spawn!");
        }
    }
}
