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
    [SerializeField] private int fontSize = 36;
    [SerializeField] private float textOffsetY = 30f; // Y offset from health bar
    
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
            ShowDamageText(damage, isRhythmHit);
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
    /// Shows a floating damage text - created entirely through code
    /// </summary>
    private void ShowDamageText(int damage, bool isRhythmHit)
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
        GameObject damageTextObj = new GameObject("DamageText");
        damageTextObj.transform.SetParent(parent, false);
        
        // Set position relative to health bar
        RectTransform rectTransform = damageTextObj.AddComponent<RectTransform>();
        rectTransform.position = transform.position + new Vector3(0, textOffsetY, 0);
        rectTransform.sizeDelta = new Vector2(200, 50); // Width and height
        
        // Add either TextMeshProUGUI (if available) or regular Text component
        // First try to use TextMeshProUGUI if available
        bool usingTMP = false;
        
        // Format text content
        string damageString = damage.ToString();
        Color textColor;
        if (isRhythmHit)
        {
            textColor = rhythmDamageColor;
            damageString = $"RHYTHM! {damageString}";
        }
        else if (damage >= 20) // Arbitrary threshold for "critical" hits
        {
            textColor = criticalDamageColor;
        }
        else
        {
            textColor = normalDamageColor;
        }
        
        // Try to use TextMeshPro if available
        Type tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        if (tmpType != null)
        {
            // TextMeshPro is available, use it
            var textComponent = damageTextObj.AddComponent(tmpType) as Component;
            
            // Set common properties via reflection
            var textProperty = tmpType.GetProperty("text");
            var colorProperty = tmpType.GetProperty("color");
            var fontSizeProperty = tmpType.GetProperty("fontSize");
            var alignmentProperty = tmpType.GetProperty("alignment");
            
            if (textProperty != null) textProperty.SetValue(textComponent, damageString);
            if (colorProperty != null) colorProperty.SetValue(textComponent, textColor);
            if (fontSizeProperty != null) fontSizeProperty.SetValue(textComponent, fontSize);
            
            // Center alignment (assumes TMPro.TextAlignmentOptions.Center = 514)
            if (alignmentProperty != null) alignmentProperty.SetValue(textComponent, 514); 
            
            usingTMP = true;
        }
        
        // Fallback to regular Unity UI Text if TextMeshPro not available
        if (!usingTMP)
        {
            Text textComponent = damageTextObj.AddComponent<Text>();
            textComponent.text = damageString;
            textComponent.color = textColor;
            textComponent.font = textFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = fontSize;
            textComponent.alignment = TextAnchor.MiddleCenter;
        }
        
        // Start animation
        StartCoroutine(AnimateDamageText(damageTextObj));
    }
    
    /// <summary>
    /// Animates the damage text floating up and fading out
    /// </summary>
    private IEnumerator AnimateDamageText(GameObject textObj)
    {
        if (textObj == null)
            yield break;
            
        float duration = 1.0f;
        float elapsed = 0f;
        
        // Get the text component (either TextMeshProUGUI or regular Text)
        Component textComponent = textObj.GetComponent(typeof(Component).Assembly.GetType("TMPro.TextMeshProUGUI")) 
                                ?? (Component)textObj.GetComponent<Text>();
        
        if (textComponent == null)
        {
            Destroy(textObj);
            yield break;
        }
        
        // Get the color property through reflection to handle both text types
        System.Reflection.PropertyInfo colorProperty = textComponent.GetType().GetProperty("color");
        if (colorProperty == null)
        {
            Destroy(textObj);
            yield break;
        }
        
        // Get starting position and scale
        Vector3 startPosition = textObj.transform.position;
        Vector3 targetPosition = startPosition + new Vector3(0, 50f, 0);
        Vector3 startScale = textObj.transform.localScale;
        Vector3 peakScale = startScale * 1.5f;
        
        // Animate
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            
            // Move up
            textObj.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            
            // Scale up then down
            if (t < 0.3f)
            {
                float scaleT = t / 0.3f;
                textObj.transform.localScale = Vector3.Lerp(startScale, peakScale, scaleT);
            }
            else
            {
                float scaleT = (t - 0.3f) / 0.7f;
                textObj.transform.localScale = Vector3.Lerp(peakScale, startScale * 0.5f, scaleT);
            }
            
            // Fade out in second half
            if (t > 0.5f)
            {
                // Get current color
                Color currentColor = (Color)colorProperty.GetValue(textComponent);
                
                // Modify alpha
                currentColor.a = Mathf.Lerp(1f, 0f, (t - 0.5f) * 2f);
                
                // Set new color
                colorProperty.SetValue(textComponent, currentColor);
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
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
