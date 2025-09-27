using UnityEngine;
using UnityEngine.UI;

public class DashCooldownUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image cooldownFill; // The fill image for the cooldown bar
    [SerializeField] private Image cooldownBackground; // Background of the cooldown indicator
    [SerializeField] private Text cooldownText; // Text showing remaining time
    
    [Header("Visual Settings")]
    [SerializeField] private Color barColor = Color.white; // Always white bar
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.3f); // Semi-transparent background
    [SerializeField] private bool showText = true;
    [SerializeField] private bool useCircularProgress = true;
    
    [Header("Font Settings")]
    [SerializeField] private Font customFont; // Font that can be assigned in Unity Inspector
    [SerializeField] private int fontSize = 16;
    
    [Header("Position Settings")]
    [SerializeField] private float verticalOffset = 0f; // Vertical position offset (positive = up, negative = down)
    [SerializeField] private bool rightAlign = true; // Right align the UI element
    
    [Header("Animation Settings")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseScale = 1.2f;
    
    private PlayerMovement playerMovement;
    private float originalScale;
    private bool isReady = true;
    
    void Start()
    {
        // Find the player movement component
        playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("DashCooldownUI: No PlayerMovement found in scene!");
            enabled = false;
            return;
        }
        
        // Get original scale for pulsing animation
        originalScale = transform.localScale.x;
        
        // Initialize UI
        if (cooldownFill != null)
        {
            cooldownFill.fillAmount = 1f;
            cooldownFill.color = barColor; // Always white
        }
        
        if (cooldownBackground != null)
        {
            cooldownBackground.color = backgroundColor;
        }
        
        if (cooldownText != null)
        {
            cooldownText.text = "READY";
            cooldownText.color = Color.white;
            
            // Apply custom font if assigned
            if (customFont != null)
            {
                cooldownText.font = customFont;
            }
            cooldownText.fontSize = fontSize;
        }
        
        // Set up right alignment
        SetupRightAlignment();
    }
    
    void SetupRightAlignment()
    {
        if (rightAlign)
        {
            // Get the RectTransform component
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // Set anchor to top-right
                rectTransform.anchorMin = new Vector2(1f, 1f);
                rectTransform.anchorMax = new Vector2(1f, 1f);
                rectTransform.pivot = new Vector2(1f, 1f);
                
                // Position with vertical offset
                rectTransform.anchoredPosition = new Vector2(-10f, -10f + verticalOffset);
            }
        }
    }
    
    void Update()
    {
        if (playerMovement == null) return;
        
        // Update position if vertical offset changed
        UpdatePosition();
        
        // Get cooldown information from player movement
        float cooldownRemaining = GetDashCooldownRemaining();
        float cooldownTotal = GetDashCooldownTotal();
        bool isDashing = playerMovement.IsDashing();
        
        // Calculate cooldown progress (0 = ready, 1 = just used)
        float cooldownProgress = cooldownTotal > 0 ? (cooldownTotal - cooldownRemaining) / cooldownTotal : 0f;
        
        // Update visual elements
        UpdateCooldownVisual(cooldownProgress, cooldownRemaining, isDashing);
        
        // Update pulsing animation when ready
        if (isReady && !isDashing)
        {
            UpdatePulseAnimation();
        }
    }
    
    void UpdatePosition()
    {
        if (rightAlign)
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // Update position with current vertical offset
                rectTransform.anchoredPosition = new Vector2(-10f, -10f + verticalOffset);
            }
        }
    }
    
    void UpdateCooldownVisual(float progress, float remaining, bool isDashing)
    {
        // Update fill amount
        if (cooldownFill != null)
        {
            if (useCircularProgress)
            {
                cooldownFill.fillAmount = 1f - progress; // Invert for circular fill
            }
            else
            {
                cooldownFill.fillAmount = progress; // Normal fill for bar
            }
        }
        
        // Update ready state
        bool wasReady = isReady;
        
        if (isDashing)
        {
            isReady = false;
        }
        else if (remaining <= 0f)
        {
            isReady = true;
        }
        else
        {
            isReady = false;
        }
        
        // Apply colors - always white bar
        if (cooldownFill != null)
        {
            cooldownFill.color = barColor; // Always white
        }
        
        if (cooldownBackground != null)
        {
            cooldownBackground.color = backgroundColor; // Semi-transparent background
        }
        
        // Update text
        if (cooldownText != null && showText)
        {
            if (isDashing)
            {
                cooldownText.text = "DASHING";
            }
            else if (remaining <= 0f)
            {
                cooldownText.text = "READY";
            }
            else
            {
                cooldownText.text = remaining.ToString("F1") + "s";
            }
            
            cooldownText.color = Color.white;
        }
        
        // Reset scale when becoming ready
        if (isReady && !wasReady)
        {
            transform.localScale = Vector3.one * originalScale;
        }
    }
    
    void UpdatePulseAnimation()
    {
        if (isReady)
        {
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.1f + 1f;
            transform.localScale = Vector3.one * originalScale * pulse;
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
    
    // Public method to get cooldown state
    public bool IsDashReady()
    {
        return isReady;
    }
    
    // Public method to get cooldown progress (0 = ready, 1 = just used)
    public float GetCooldownProgress()
    {
        float cooldownRemaining = GetDashCooldownRemaining();
        float cooldownTotal = GetDashCooldownTotal();
        return cooldownTotal > 0 ? (cooldownTotal - cooldownRemaining) / cooldownTotal : 0f;
    }
}
