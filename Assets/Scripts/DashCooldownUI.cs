using UnityEngine;
using UnityEngine.UI;

public class DashCooldownUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image cooldownFill; // The fill image for the cooldown bar
    [SerializeField] private Image cooldownBackground; // Background of the cooldown indicator
    [SerializeField] private Text cooldownText; // Text showing remaining time
    
    [Header("Visual Settings")]
    [SerializeField] private Color readyColor = Color.green;
    [SerializeField] private Color cooldownColor = Color.red;
    [SerializeField] private Color chargingColor = Color.yellow;
    [SerializeField] private bool showText = true;
    [SerializeField] private bool useCircularProgress = true;
    
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
            cooldownFill.color = readyColor;
        }
        
        if (cooldownBackground != null)
        {
            cooldownBackground.color = readyColor;
        }
        
        if (cooldownText != null)
        {
            cooldownText.text = "READY";
            cooldownText.color = Color.white;
        }
    }
    
    void Update()
    {
        if (playerMovement == null) return;
        
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
        
        // Update colors based on state
        Color currentColor;
        bool wasReady = isReady;
        
        if (isDashing)
        {
            currentColor = chargingColor;
            isReady = false;
        }
        else if (remaining <= 0f)
        {
            currentColor = readyColor;
            isReady = true;
        }
        else
        {
            currentColor = cooldownColor;
            isReady = false;
        }
        
        // Apply colors
        if (cooldownFill != null)
        {
            cooldownFill.color = currentColor;
        }
        
        if (cooldownBackground != null)
        {
            cooldownBackground.color = Color.Lerp(currentColor, Color.white, 0.3f);
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
