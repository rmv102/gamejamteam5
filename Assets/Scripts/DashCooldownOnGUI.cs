using UnityEngine;

public class DashCooldownOnGUI : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private bool showCooldownBar = true;
    [SerializeField] private bool showCooldownText = true;
    
    [Header("Position & Size")]
    [SerializeField] private Vector2 barSize = new Vector2(200, 20);
    [SerializeField] private float margin = 20f; // Distance from screen edges
    
    [Header("Font Settings")]
    [SerializeField] private Font customFont; // Font that can be assigned in Unity Inspector
    [SerializeField] private int fontSize = 16;
    
    [Header("Colors")]
    [SerializeField] private Color barColor = Color.white;
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.3f);
    [SerializeField] private Color textColor = Color.white;
    
    private PlayerMovement playerMovement;
    private GUIStyle textStyle;
    private GUIStyle barStyle;
    
    void Start()
    {
        // Find the player movement component
        playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("DashCooldownOnGUI: No PlayerMovement found in scene!");
            enabled = false;
            return;
        }
        
        // Initialize GUI styles
        textStyle = new GUIStyle();
        textStyle.fontSize = fontSize;
        textStyle.fontStyle = FontStyle.Bold;
        textStyle.normal.textColor = textColor;
        
        // Apply custom font if assigned
        if (customFont != null)
        {
            textStyle.font = customFont;
        }
        
        barStyle = new GUIStyle();
    }
    
    void OnGUI()
    {
        if (playerMovement == null) return;
        
        // Get cooldown information
        float cooldownRemaining = GetDashCooldownRemaining();
        float cooldownTotal = GetDashCooldownTotal();
        bool isDashing = playerMovement.IsDashing();
        
        // Calculate cooldown progress
        float cooldownProgress = cooldownTotal > 0 ? (cooldownTotal - cooldownRemaining) / cooldownTotal : 0f;
        
        // Calculate position for bottom right corner
        float barX = Screen.width - barSize.x - margin;
        float barY = Screen.height - barSize.y - margin;
        Vector2 barPosition = new Vector2(barX, barY);
        
        // Draw cooldown bar
        if (showCooldownBar)
        {
            DrawCooldownBar(barPosition, cooldownProgress, isDashing);
        }
        
        // Draw cooldown text
        if (showCooldownText)
        {
            DrawCooldownText(barPosition, cooldownRemaining, isDashing);
        }
    }
    
    void DrawCooldownBar(Vector2 position, float progress, bool isDashing)
    {
        // Background
        Rect backgroundRect = new Rect(position.x, position.y, barSize.x, barSize.y);
        GUI.color = backgroundColor;
        GUI.DrawTexture(backgroundRect, Texture2D.whiteTexture);
        
        // Progress fill - always white
        Rect fillRect = new Rect(position.x, position.y, barSize.x * progress, barSize.y);
        GUI.color = barColor;
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        
        // Reset color
        GUI.color = Color.white;
    }
    
    void DrawCooldownText(Vector2 barPosition, float remaining, bool isDashing)
    {
        string text;
        
        if (isDashing)
        {
            text = "DASHING!";
        }
        else if (remaining <= 0f)
        {
            text = "DASH READY";
        }
        else
        {
            text = $"DASH: {remaining:F1}s";
        }
        
        // Position text above the bar
        float textX = barPosition.x;
        float textY = barPosition.y - 25f; // 25 pixels above the bar
        
        textStyle.normal.textColor = textColor;
        GUI.Label(new Rect(textX, textY, barSize.x, 30), text, textStyle);
    }
    
    
    float GetDashCooldownRemaining()
    {
        // Use reflection to access private field
        var field = typeof(PlayerMovement).GetField("dashCooldownTimer", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? (float)field.GetValue(playerMovement) : 0f;
    }
    
    float GetDashCooldownTotal()
    {
        // Use reflection to access private field
        var field = typeof(PlayerMovement).GetField("dashCooldown", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        return field != null ? (float)field.GetValue(playerMovement) : 1f;
    }
}
