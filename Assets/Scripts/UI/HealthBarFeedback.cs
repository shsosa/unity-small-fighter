using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Enhances health bar with visual feedback when damage is taken
/// </summary>
public class HealthBarFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image healthBarImage;
    [SerializeField] private Transform healthBarContainer;
    
    [Header("Damage Text")]
    [SerializeField] private Transform damageTextParent; // Optional parent transform for text
    [SerializeField] private Color normalDamageColor = Color.white;
    [SerializeField] private Color criticalDamageColor = Color.red;
    [SerializeField] private Color rhythmDamageColor = new Color(1f, 0.5f, 0f); // Orange
    [SerializeField] private Font textFont; // Optional custom font (will use default if null)
    [SerializeField] private int fontSize = 64; // Much larger text size
    [SerializeField] private float textOffsetY = 50f; // Bigger Y offset from health bar for more visibility
    
    [Header("Animation Settings")]
    [SerializeField] private float scaleAmount = 1.2f;
    [SerializeField] private float scaleDuration = 0.2f;
    [SerializeField] private AnimationCurve scaleCurve;
    
    private int previousHealth = 100;
    private Vector3 originalScale;
    private Coroutine scaleCoroutine;
    
    private void Start()
    {
        // Store original scale for animations
        if (healthBarContainer != null)
        {
            originalScale = healthBarContainer.localScale;
        }
        else
        {
            originalScale = transform.localScale;
            healthBarContainer = transform;
        }
        
        // Create scale curve if none exists
        if (scaleCurve == null || scaleCurve.keys.Length == 0)
        {
            scaleCurve = new AnimationCurve(
                new Keyframe(0, 0, 2, 2),
                new Keyframe(0.3f, 1.2f, 0, 0),
                new Keyframe(0.7f, 0.9f, 0, 0),
                new Keyframe(1, 1, 2, 2)
            );
        }
    }
    
    /// <summary>
    /// Called when a fighter takes damage
    /// </summary>
    /// <param name="currentHealth">Current health value</param>
    /// <param name="maxHealth">Maximum health value</param>
    /// <param name="isRhythmHit">Whether this was a rhythm-enhanced hit</param>
    public void OnHealthChanged(int currentHealth, int maxHealth, bool isRhythmHit = false)
    {
        // Calculate damage taken
        int damage = previousHealth - currentHealth;
        
        // Only show feedback for damage (not healing)
        if (damage > 0)
        {
            // Play scale animation
            PlayScaleAnimation();
            
            // Show damage text
            bool isCritical = damage >= 20;
            ShowDamageText(damage, isRhythmHit, isCritical);
        }
        
        // Update previous health
        previousHealth = currentHealth;
        
        // Update health bar if not already handled elsewhere
        if (healthBarImage != null)
        {
            healthBarImage.fillAmount = (float)currentHealth / maxHealth;
        }
    }
    
    /// <summary>
    /// Shows animated damage text
    /// </summary>
    /// <param name="damage">Amount of damage to show</param>
    /// <param name="isRhythmHit">Whether this was a rhythm-enhanced hit</param>
    /// <param name="isCritical">Whether this was a critical hit</param>
    private void ShowDamageText(int damage, bool isRhythmHit = false, bool isCritical = false)
    {
        // Determine parent for the text (canvas is required)
        Transform parent = damageTextParent;
        if (parent == null)
        {
            // Find a canvas in the scene if no parent specified
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                parent = canvas.transform;
            }
            else
            {
                Debug.LogError("HealthBarFeedback: No canvas found for damage text");
                return;
            }
        }
        
        // Create text game object
        GameObject damageTextObj = new GameObject("DamageText_" + damage.ToString());
        damageTextObj.transform.SetParent(parent, false);
        
        // Add RectTransform first (crucial for UI positioning)
        RectTransform rectTransform = damageTextObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        
        // Make it large enough
        rectTransform.sizeDelta = new Vector2(200f, 100f); 
        
        // Position at health bar with offset for visibility
        RectTransform healthBarRect = healthBarImage?.GetComponent<RectTransform>();
        if (healthBarRect != null)
        {
            rectTransform.position = healthBarRect.position + new Vector3(0, 60f, 0);
        }
        else
        {
            rectTransform.position = transform.position + new Vector3(0, 60f, 0);
        }
        
        // Format text content with prefixes for special hits
        string damageString = damage.ToString();
        if (isRhythmHit)
        {
            damageString = "RHYTHM! " + damageString;
        }
        else if (isCritical)
        {
            damageString = "CRIT! " + damageString;
        }
        
        // Choose color based on hit type
        Color textColor = isRhythmHit ? rhythmDamageColor : 
                        (isCritical ? criticalDamageColor : normalDamageColor);
        
        // Try to use TextMeshPro first
        bool usingTMP = false;
        Type tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        
        if (tmpType != null)
        {
            // Create TMPro text component
            Component textComponent = damageTextObj.AddComponent(tmpType);
            
            // Set text properties via reflection
            var textProp = tmpType.GetProperty("text");
            var colorProp = tmpType.GetProperty("color");
            var sizeProp = tmpType.GetProperty("fontSize");
            var alignProp = tmpType.GetProperty("alignment");
            var styleProp = tmpType.GetProperty("fontStyle");
            
            if (textProp != null) textProp.SetValue(textComponent, damageString);
            if (colorProp != null) colorProp.SetValue(textComponent, textColor);
            if (sizeProp != null) sizeProp.SetValue(textComponent, fontSize);
            
            // Center alignment (using enum value)
            if (alignProp != null)
            {
                try
                {
                    var alignEnum = Enum.Parse(Type.GetType("TMPro.TextAlignmentOptions, Unity.TextMeshPro"), "Center");
                    alignProp.SetValue(textComponent, alignEnum);
                }
                catch { } // Ignore if we can't set alignment
            }
            
            // Try to make it bold
            if (styleProp != null)
            {
                try
                {
                    var fontStyleEnum = Enum.Parse(styleProp.PropertyType, "Bold");
                    styleProp.SetValue(textComponent, fontStyleEnum);
                }
                catch { } // Ignore if we can't set style
            }
            
            usingTMP = true;
        }
        
        // Fallback to regular Text if TMPro isn't available
        if (!usingTMP)
        {
            Text textComponent = damageTextObj.AddComponent<Text>();
            textComponent.text = damageString;
            textComponent.color = textColor;
            textComponent.font = textFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = fontSize;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.fontStyle = FontStyle.Bold;
        }
        
        // Add outline/shadow for better visibility
        try
        {
            var outlineType = Type.GetType("TMPro.TMP_Text+TextEffectGroup, Unity.TextMeshPro");
            if (outlineType != null && usingTMP)
            {
                var textComp = damageTextObj.GetComponent(tmpType);
                var outlineProp = tmpType.GetProperty("outlineWidth");
                if (outlineProp != null)
                {
                    outlineProp.SetValue(textComp, 0.2f);
                }
            }
        }
        catch { } // Ignore outline errors
        
        // Start the animation with string value and color
        StartCoroutine(AnimateDamageText(damageTextObj));
    }
    
    /// <summary>
    /// Creates a quick pop-in scale effect for text
    /// </summary>
    private IEnumerator PopInEffect(GameObject textObj, Vector3 targetScale)
    {
        if (textObj == null) yield break;
        
        // Start small
        textObj.transform.localScale = targetScale * 0.5f;
        
        // Pop to large size
        float popDuration = 0.1f;
        float popElapsed = 0f;
        
        // Overshoot scale
        Vector3 overshootScale = targetScale * 1.5f;
        
        while (popElapsed < popDuration)
        {
            float t = popElapsed / popDuration;
            // Ease out quad for quick pop
            float easedT = t * (2 - t);
            textObj.transform.localScale = Vector3.Lerp(targetScale * 0.5f, overshootScale, easedT);
            
            popElapsed += Time.deltaTime;
            yield return null;
        }
        
        // Settle back to normal size
        popDuration = 0.1f;
        popElapsed = 0f;
        
        while (popElapsed < popDuration)
        {
            float t = popElapsed / popDuration;
            // Ease in out
            float easedT = t < 0.5f ? 2 * t * t : 1 - (float)Math.Pow(-2 * t + 2, 2) / 2;
            textObj.transform.localScale = Vector3.Lerp(overshootScale, targetScale, easedT);
            
            popElapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final scale is set
        textObj.transform.localScale = targetScale;
    }
    
    /// <summary>
    /// Animations damage text - SUPERCHARGED for extreme visibility
    /// </summary>
    private IEnumerator AnimateDamageText(GameObject textObj)
    {
        if (textObj == null) yield break;
        
        // Make sure text is at the front of the UI hierarchy
        textObj.transform.SetAsLastSibling();
        
        // Find text component
        Component textComponent = textObj.GetComponent("TextMeshProUGUI") ?? 
                                 textObj.GetComponent<Text>();
        if (textComponent == null)
        {
            Destroy(textObj);
            yield break;
        }
        
        // Get color property for fading
        var colorProperty = textComponent.GetType().GetProperty("color");
        if (colorProperty == null)
        {
            Destroy(textObj);
            yield break;
        }
        
        // Store original position/scale
        RectTransform rectTransform = textObj.GetComponent<RectTransform>();
        Vector2 startPosition = rectTransform.anchoredPosition;
        Vector3 startScale = textObj.transform.localScale;
        
        // Animation parameters
        float duration = 1.8f;
        float elapsed = 0f;
        
        // EXTREME movement values - visible for UI space
        float velocityY = 600f;      // Very high upward velocity
        float gravity = -2000f;     // Strong gravity for fast fall
        float velocityX = UnityEngine.Random.Range(-300f, 300f); // Random horizontal
        
        // Initial pop animation
        float popDuration = 0.1f;
        for (float t = 0; t < popDuration; t += Time.deltaTime)
        {
            float normalized = t / popDuration;
            textObj.transform.localScale = Vector3.Lerp(
                startScale * 0.5f, 
                startScale * 1.5f, // Extra large pop
                Mathf.SmoothStep(0, 1, normalized)
            );
            yield return null;
        }
        
        // Reduce scale slightly after pop
        textObj.transform.localScale = startScale * 1.2f;
        
        // Current animation position relative to start
        float posY = 0;
        float posX = 0;
        Vector2 currentPos = startPosition;
        
        // Main physics animation loop
        while (elapsed < duration)
        {
            float deltaTime = Time.deltaTime;
            elapsed += deltaTime;
            
            // Apply velocity and gravity (scaled for UI space)
            velocityY += gravity * deltaTime;
            posY += velocityY * deltaTime;
            posX += velocityX * deltaTime;
            
            // Bounce off "floor"
            if (posY < -100f && velocityY < 0) // Use larger value for UI space
            {
                posY = -100f;
                velocityY = -velocityY * 0.6f; // 60% bounce energy
                velocityX *= 0.5f;             // Slow horizontal on bounce
                
                // Add impact effect
                textObj.transform.localScale = startScale * 1.3f;
                
                // Stop bouncing if velocity is too low
                if (velocityY < 200f)
                {
                    velocityY = 0;
                    posY = -100f;
                }
            }
            
            // CRITICAL: Use anchoredPosition for UI movement
            rectTransform.anchoredPosition = startPosition + new Vector2(posX, posY);
            
            // Dynamic rotation based on velocity
            float tiltAmount = Mathf.Clamp(-velocityY / 100f, -20f, 20f);
            textObj.transform.rotation = Quaternion.Euler(0, 0, tiltAmount);
            
            // Add subtle scale pulse
            float baseScale = Mathf.Lerp(1.2f, 1.0f, elapsed / duration);
            float pulse = baseScale + 0.1f * Mathf.Sin(elapsed * 15f);
            textObj.transform.localScale = startScale * pulse;
            
            // Fade out in the latter part of animation
            if (elapsed > duration * 0.6f)
            {
                float fadeRatio = (elapsed - (duration * 0.6f)) / (duration * 0.4f);
                Color currentColor = (Color)colorProperty.GetValue(textComponent);
                currentColor.a = Mathf.Clamp01(1f - fadeRatio);
                colorProperty.SetValue(textComponent, currentColor);
            }
            
            yield return null;
        }
        
        // Clean up
        Destroy(textObj);
    }
    
    /// <summary>
    /// Plays a scaling animation on the health bar
    /// </summary>
    private void PlayScaleAnimation()
    {
        if (healthBarContainer == null)
            return;
            
        // Stop any existing animation
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        
        // Start new animation
        scaleCoroutine = StartCoroutine(ScaleCoroutine());
    }
    
    /// <summary>
    /// Coroutine for health bar scale animation
    /// </summary>
    private IEnumerator ScaleCoroutine()
    {
        float elapsed = 0f;
        
        while (elapsed < scaleDuration)
        {
            float t = elapsed / scaleDuration;
            float curveValue = scaleCurve.Evaluate(t);
            
            // Apply scale with animation curve
            Vector3 targetScale = Vector3.Lerp(
                originalScale, 
                originalScale * scaleAmount, 
                curveValue
            );
            
            healthBarContainer.localScale = targetScale;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we end at original scale
        healthBarContainer.localScale = originalScale;
        scaleCoroutine = null;
    }
}
