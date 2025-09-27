using UnityEngine;

public class DashCooldownOnGUI : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private bool showCooldownBar = true;
    [SerializeField] private bool showCooldownText = true;
    [SerializeField] private bool showDashIndicator = true;
    
    [Header("Position & Size")]
    [SerializeField] private Vector2 barPosition = new Vector2(20, 20);
    [SerializeField] private Vector2 barSize = new Vector2(200, 20);
    [SerializeField] private Vector2 textPosition = new Vector2(20, 50);
    
    [Header("Colors")]
    [SerializeField] private Color readyColor = Color.green;
    [SerializeField] private Color cooldownColor = Color.red;
    [SerializeField] private Color chargingColor = Color.yellow;
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.5f);
    
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
        textStyle.fontSize = 16;
        textStyle.fontStyle = FontStyle.Bold;
        textStyle.normal.textColor = Color.white;
        
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
        
        // Draw cooldown bar
        if (showCooldownBar)
        {
            DrawCooldownBar(cooldownProgress, isDashing);
        }
        
        // Draw cooldown text
        if (showCooldownText)
        {
            DrawCooldownText(cooldownRemaining, isDashing);
        }
        
        // Draw dash indicator
        if (showDashIndicator)
        {
            DrawDashIndicator(isDashing);
        }
    }
    
    void DrawCooldownBar(float progress, bool isDashing)
    {
        // Background
        Rect backgroundRect = new Rect(barPosition.x, barPosition.y, barSize.x, barSize.y);
        GUI.color = backgroundColor;
        GUI.DrawTexture(backgroundRect, Texture2D.whiteTexture);
        
        // Progress fill
        Rect fillRect = new Rect(barPosition.x, barPosition.y, barSize.x * progress, barSize.y);
        
        if (isDashing)
        {
            GUI.color = chargingColor;
        }
        else if (progress >= 1f)
        {
            GUI.color = readyColor;
        }
        else
        {
            GUI.color = cooldownColor;
        }
        
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        
        // Border
        GUI.color = Color.white;
        GUI.DrawTexture(backgroundRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, Color.white, 2, 2);
        
        // Reset color
        GUI.color = Color.white;
    }
    
    void DrawCooldownText(float remaining, bool isDashing)
    {
        string text;
        Color textColor = Color.white;
        
        if (isDashing)
        {
            text = "DASHING!";
            textColor = chargingColor;
        }
        else if (remaining <= 0f)
        {
            text = "DASH READY";
            textColor = readyColor;
        }
        else
        {
            text = $"DASH: {remaining:F1}s";
            textColor = cooldownColor;
        }
        
        textStyle.normal.textColor = textColor;
        GUI.Label(new Rect(textPosition.x, textPosition.y, 200, 30), text, textStyle);
    }
    
    void DrawDashIndicator(bool isDashing)
    {
        if (isDashing)
        {
            // Draw a pulsing indicator when dashing
            float pulse = Mathf.Sin(Time.time * 10f) * 0.5f + 0.5f;
            GUI.color = new Color(chargingColor.r, chargingColor.g, chargingColor.b, pulse);
            
            Rect indicatorRect = new Rect(Screen.width - 50, 20, 30, 30);
            GUI.DrawTexture(indicatorRect, Texture2D.whiteTexture);
            
            GUI.color = Color.white;
        }
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
