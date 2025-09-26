using UnityEngine;

public class VirusInfection : MonoBehaviour
{
    // We will use this to change the color of the veins
    public Color infectedColor = Color.green;

    // This method is called when the player's collider enters another collider
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object we collided with has the InfectionController script
        InfectionController vein = other.GetComponent<InfectionController>();

        // If it does, it's a vein we can infect
        if (vein != null)
        {
            // Get the SpriteRenderer of the vein
            SpriteRenderer veinRenderer = other.GetComponent<SpriteRenderer>();

            // Change the color of the vein to the infected color
            if (veinRenderer != null)
            {
                veinRenderer.color = infectedColor;
            }
        }
    }
}
