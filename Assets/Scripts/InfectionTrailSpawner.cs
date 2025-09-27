using UnityEngine;

public class InfectedParticleDropper : MonoBehaviour
{
    // --- Public Fields (Set these in the Inspector) ---

    // 1. Drag your "Infected Particle" prefab here.
    public GameObject particlePrefab;

    // 2. The distance the player must move before TRYING to drop a particle.
    public float dropDistance = 0.5f;

    // 3. The minimum distance allowed between particles. No particle will spawn inside this radius of another.
    public float clearanceRadius = 1.0f;

    // 4. IMPORTANT: Set the Tag of your particle prefab to this string in the Inspector.
    public string particleTag = "InfectedParticle";


    // --- Private Fields ---
    private Vector3 lastDropPosition;

    void Start()
    {
        // Initialize the last position to the player's starting position.
        lastDropPosition = transform.position;

        // A quick check to help you debug if the prefab is set up correctly.
        if (particlePrefab != null && particlePrefab.CompareTag(particleTag) == false)
        {
            Debug.LogWarning("The 'particlePrefab' does not have the tag '" + particleTag + "'. The clearance check might not work correctly. Please add the tag to the prefab.");
        }
    }

    void Update()
    {
        // Check the distance the player has moved since the last drop attempt.
        if (Vector3.Distance(transform.position, lastDropPosition) >= dropDistance)
        {
            // Before spawning, check if the area is clear of other particles.
            if (IsAreaClear())
            {
                // Spawn a new particle at the current position.
                Instantiate(particlePrefab, transform.position, Quaternion.identity);
            }

            // IMPORTANT: Update the last position regardless of whether we spawned a particle.
            // This prevents the script from checking every frame when the player is near an existing particle.
            lastDropPosition = transform.position;
        }
    }

    // This function checks if there are any particles with the correct tag nearby.
    private bool IsAreaClear()
    {
        // Get all colliders within the clearance radius. This check is layer-agnostic.
        Collider2D[] collidersInRadius = Physics2D.OverlapCircleAll(transform.position, clearanceRadius);

        // Loop through every collider we found.
        foreach (Collider2D col in collidersInRadius)
        {
            // If any of the found colliders has the correct tag...
            if (col.CompareTag(particleTag))
            {
                // ...then the area is NOT clear. Return false and stop checking.
                return false;
            }
        }

        // If we looped through all colliders and found none with the tag, the area is clear.
        return true;
    }

    // Optional: This will draw a helpful wireframe circle in the Scene view so you can visualize the clearanceRadius.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, clearanceRadius);
    }
}

