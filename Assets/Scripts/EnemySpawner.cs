using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Spawning Settings")]
    [SerializeField] GameObject enemyPrefab; // Drag the Enemy prefab here
    [SerializeField] int maxEnemies = 5; // Maximum number of enemies in scene
    [SerializeField] Transform[] spawnPoints; // Array of spawn point transforms
    [SerializeField] float spawnInterval = 3f; // Base time between spawn attempts
    [SerializeField] float spawnIntervalRandomness = 1f; // Random variation in spawn timing
    
    [Header("Spawn Cooldown Settings")]
    [SerializeField] float minSpawnCooldown = 2f; // Minimum time between any spawns
    [SerializeField] float killSpawnCooldown = 1.5f; // Extra cooldown when enemy is killed
    
    [Header("Debug")]
    [SerializeField] bool showDebugInfo = true;
    [SerializeField] bool showSpawnPoints = true;
    
    private Transform player;
    private float lastSpawnTime;
    private float nextSpawnTime;
    private int currentEnemyCount = 0;
    private int previousEnemyCount = 0;
    private float lastKillTime = 0f;
    
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
        
        // Validate spawn points
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("EnemySpawner: No spawn points assigned! Please assign spawn point GameObjects to the Spawn Points array.");
        }
        else
        {
            int validSpawnPoints = 0;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                {
                    validSpawnPoints++;
                    if (showDebugInfo)
                    {
                        Debug.Log($"EnemySpawner: Spawn point {i}: {spawnPoints[i].name} at {spawnPoints[i].position}");
                    }
                }
                else
                {
                    Debug.LogWarning($"EnemySpawner: Spawn point {i} is null!");
                }
            }
            Debug.Log($"EnemySpawner: Found {validSpawnPoints}/{spawnPoints.Length} valid spawn points");
        }
        
        Debug.Log("EnemySpawner: Initialization complete!");
    }
    
    void Update()
    {
        // Only spawn if we have an enemy prefab, player, and valid spawn points
        if (enemyPrefab == null || player == null || spawnPoints == null || spawnPoints.Length == 0) 
        {
            if (Time.frameCount % 300 == 0) // Log every 5 seconds at 60fps
            {
                Debug.LogWarning($"EnemySpawner: Cannot spawn - enemyPrefab: {(enemyPrefab != null ? "OK" : "NULL")}, player: {(player != null ? "OK" : "NULL")}, spawnPoints: {(spawnPoints != null && spawnPoints.Length > 0 ? "OK" : "NULL/EMPTY")}");
            }
            return;
        }
        
        // Count current enemies
        currentEnemyCount = FindObjectsByType<enemy_pathfinder>(FindObjectsSortMode.None).Length;
        
        // Check if an enemy was killed (count decreased)
        if (currentEnemyCount < previousEnemyCount)
        {
            lastKillTime = Time.time;
            Debug.Log($"EnemySpawner: Enemy killed! Count: {previousEnemyCount} -> {currentEnemyCount}. Kill cooldown activated.");
        }
        
        // Update previous count
        previousEnemyCount = currentEnemyCount;
        
        // Log status every 5 seconds
        if (showDebugInfo && Time.frameCount % 300 == 0)
        {
            float timeSinceKill = Time.time - lastKillTime;
            Debug.Log($"EnemySpawner: Current enemies: {currentEnemyCount}/{maxEnemies}, Time until next spawn: {(nextSpawnTime - Time.time):F1}s, Time since last kill: {timeSinceKill:F1}s");
        }
        
        // Check if we can spawn (under limit, enough time passed, and cooldowns satisfied)
        bool canSpawn = currentEnemyCount < maxEnemies && Time.time >= nextSpawnTime;
        bool minCooldownPassed = (Time.time - lastSpawnTime) >= minSpawnCooldown;
        bool killCooldownPassed = (Time.time - lastKillTime) >= killSpawnCooldown;
        
        if (canSpawn && minCooldownPassed && killCooldownPassed)
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
        else if (currentEnemyCount < maxEnemies)
        {
            // Log why we can't spawn
            if (showDebugInfo && Time.frameCount % 300 == 0)
            {
                string reason = "";
                if (!minCooldownPassed) reason += "Min cooldown not passed. ";
                if (!killCooldownPassed) reason += "Kill cooldown not passed. ";
                if (!canSpawn) reason += "Spawn time not reached. ";
                Debug.Log($"EnemySpawner: Cannot spawn yet - {reason}");
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
        // Get all valid spawn points (non-null)
        List<Transform> validSpawnPoints = new List<Transform>();
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                validSpawnPoints.Add(spawnPoints[i]);
            }
        }
        
        if (validSpawnPoints.Count == 0)
        {
            if (showDebugInfo)
            {
                Debug.LogError("EnemySpawner: No valid spawn points available!");
            }
            return Vector2.zero;
        }
        
        // Try to find a spawn point that's not occupied by another enemy
        List<Transform> availableSpawnPoints = new List<Transform>();
        foreach (Transform spawnPoint in validSpawnPoints)
        {
            if (IsPositionClear(spawnPoint.position))
            {
                availableSpawnPoints.Add(spawnPoint);
            }
        }
        
        // If no spawn points are clear, use any valid spawn point
        if (availableSpawnPoints.Count == 0)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("EnemySpawner: All spawn points are occupied, using random spawn point anyway");
            }
            availableSpawnPoints = validSpawnPoints;
        }
        
        // Randomly select a spawn point
        Transform selectedSpawnPoint = availableSpawnPoints[Random.Range(0, availableSpawnPoints.Count)];
        Vector2 spawnPosition = selectedSpawnPoint.position;
        
        if (showDebugInfo)
        {
            Debug.Log($"EnemySpawner: Selected spawn point '{selectedSpawnPoint.name}' at {spawnPosition}");
        }
        
        return spawnPosition;
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
        Debug.Log($"EnemySpawner: Spawned new enemy at {position}. Total enemies: {currentEnemyCount + 1}");
    }
    
    // Debug visualization
    void OnDrawGizmos()
    {
        if (showSpawnPoints && spawnPoints != null)
        {
            // Draw spawn points
            Gizmos.color = Color.green;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                {
                    Gizmos.DrawWireSphere(spawnPoints[i].position, 1f);
                    // Draw a small cross to make it more visible
                    Gizmos.DrawLine(spawnPoints[i].position + Vector3.up * 0.5f, spawnPoints[i].position + Vector3.down * 0.5f);
                    Gizmos.DrawLine(spawnPoints[i].position + Vector3.left * 0.5f, spawnPoints[i].position + Vector3.right * 0.5f);
                }
            }
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
        Debug.Log($"Next Spawn Time: {nextSpawnTime:F1} (Current Time: {Time.time:F1})");
        Debug.Log($"Time Until Next Spawn: {(nextSpawnTime - Time.time):F1} seconds");
        Debug.Log($"Min Spawn Cooldown: {minSpawnCooldown}s (Time since last spawn: {(Time.time - lastSpawnTime):F1}s)");
        Debug.Log($"Kill Spawn Cooldown: {killSpawnCooldown}s (Time since last kill: {(Time.time - lastKillTime):F1}s)");
        
        // Test spawn points
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("Spawn Points: NULL or EMPTY!");
        }
        else
        {
            int validSpawnPoints = 0;
            Debug.Log($"Found {spawnPoints.Length} spawn points:");
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                {
                    validSpawnPoints++;
                    bool isClear = IsPositionClear(spawnPoints[i].position);
                    Debug.Log($"  - {i}: {spawnPoints[i].name} at {spawnPoints[i].position} (Clear: {isClear})");
                }
                else
                {
                    Debug.LogWarning($"  - {i}: NULL");
                }
            }
            Debug.Log($"Valid Spawn Points: {validSpawnPoints}/{spawnPoints.Length}");
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
        
        // Try to use a spawn point if available, otherwise use a fallback position
        Vector2 spawnPos;
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            // Find first valid spawn point
            Transform validSpawnPoint = null;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                {
                    validSpawnPoint = spawnPoints[i];
                    break;
                }
            }
            
            if (validSpawnPoint != null)
            {
                spawnPos = validSpawnPoint.position;
                Debug.Log($"EnemySpawner: Using spawn point '{validSpawnPoint.name}' at {spawnPos}");
            }
            else
            {
                spawnPos = Vector2.zero;
                Debug.LogWarning("EnemySpawner: No valid spawn points found, using origin");
            }
        }
        else
        {
            // Fallback: spawn near player or at origin
            if (player != null)
            {
                spawnPos = (Vector2)player.position + Random.insideUnitCircle * 10f;
            }
            else
            {
                spawnPos = Random.insideUnitCircle * 10f;
            }
            Debug.LogWarning("EnemySpawner: No spawn points assigned, using fallback position");
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

