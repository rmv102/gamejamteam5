using UnityEngine;
using System.Collections;

public class FadeChildSprites : MonoBehaviour
{
    public Color targetColor = new Color32(0x15, 0x15, 0x15, 255); // #151515
    public float duration = 1f; // duration of fade in seconds

    private SpriteRenderer[] sprites;

    void Start()
    {
        // Get all SpriteRenderers in this object and all children
        sprites = GetComponentsInChildren<SpriteRenderer>();
        
        // Start the fade
        StartCoroutine(FadeSprites());
    }

    private IEnumerator FadeSprites()
    {
        float elapsed = 0f;

        // Store the original colors
        Color[] originalColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            originalColors[i] = sprites[i].color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i].color = Color.Lerp(originalColors[i], targetColor, t);
            }

            yield return null; // wait for next frame
        }

        // Ensure final color is exact
        for (int i = 0; i < sprites.Length; i++)
            sprites[i].color = targetColor;
    }
}
