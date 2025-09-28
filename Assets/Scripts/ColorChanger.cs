using UnityEngine;

public class ColorChanger2D : MonoBehaviour
{
    public Color newColor = new Color32(0x15, 0x15, 0x15, 255); // Choose color in Inspector

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        // Get the SpriteRenderer component attached to this GameObject
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = newColor; // Change the color
        }
        else
        {
            Debug.LogWarning("No SpriteRenderer found on this object!");
        }
    }
}