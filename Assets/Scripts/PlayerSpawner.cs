using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public Transform[] spawnPoints;      // Assign spawn points in order
    private int currentSpawnIndex = 0;   // Tracks which spawn point to use
    private GameObject player;           // Reference to the player

    void Start()
    {
        // Find the player in the scene
        player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("Player not found in scene!");
            return;
        }

        // Place the player at the first spawn point
        player.transform.position = spawnPoints[currentSpawnIndex].position;
        player.transform.rotation = spawnPoints[currentSpawnIndex].rotation;
    }

    public void MovePlayerToNextSpawn()
    {
        currentSpawnIndex++;

        if (currentSpawnIndex >= spawnPoints.Length)
        {
            Debug.Log("No more spawn points!");
            return; // Stop if no more spawns
        }

        // Teleport the player to the next spawn
        player.transform.position = spawnPoints[currentSpawnIndex].position;
        player.transform.rotation = spawnPoints[currentSpawnIndex].rotation;
    }
}
