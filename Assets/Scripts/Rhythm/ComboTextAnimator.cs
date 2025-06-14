using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Handles animation effects for combo text in the rhythm system.
/// This component animates the combo counter text with popup effects.
/// </summary>
public class ComboTextAnimator : MonoBehaviour
{
    public TextMeshProUGUI comboText;
    public float popupDuration = 0.5f;
    public float popupScale = 1.5f;
    public float displayDuration = 3.0f; // How long the combo text stays visible before fading out

    private SimpleRhythmFighter rhythmFighter;
    private int lastComboCount = 0;
    private Vector3 originalScale;
    private Coroutine animationCoroutine;

    private void Start()
    {
        // Get reference to the fighter
        rhythmFighter = GetComponent<SimpleRhythmFighter>();
        
        if (comboText != null)
        {
            originalScale = comboText.transform.localScale;
            
            // Initialize with empty text
            comboText.text = "";
            
            // Start monitoring combo changes
            StartCoroutine(MonitorComboChanges());
        }
    }

    private IEnumerator MonitorComboChanges()
    {
        while (true)
        {
            // Check if we have all references
            if (rhythmFighter != null && comboText != null)
            {
                // Check for combo changes
                if (rhythmFighter.comboCount != lastComboCount)
                {
                    // Update combo text
                    if (rhythmFighter.comboCount >= 1)
                    {
                        comboText.text = "Beat Combo " + rhythmFighter.comboCount;
                        
                        // Animate the text
                        if (animationCoroutine != null)
                        {
                            StopCoroutine(animationCoroutine);
                        }
                        animationCoroutine = StartCoroutine(AnimateComboText());
                        
                        // Start fade out timer
                        StartCoroutine(FadeOutComboTextAfterDelay());
                    }
                    else
                    {
                        comboText.text = "";
                    }
                    
                    lastComboCount = rhythmFighter.comboCount;
                }
            }
            
            yield return new WaitForSeconds(0.05f); // Check frequently but not every frame
        }
    }

    private IEnumerator AnimateComboText()
    {
        // Store initial position and scale
        Vector3 startPosition = comboText.transform.position;
        
        // Physics simulation parameters - MUCH more dramatic movement
        float duration = 1.5f;  // Longer duration to see the full jump
        float elapsed = 0f;
        float initialVerticalVelocity = 500f;  // Much higher initial jump
        float gravity = -1200f;                // Stronger gravity for faster fall
        float horizontalVelocity = Random.Range(-50f, 50f);  // More horizontal movement
        float currentVerticalVelocity = initialVerticalVelocity;
        float currentVerticalPosition = 0f;
        float horizontalPosition = 0f;
        
        // First do a quick pop-in scale effect
        StartCoroutine(PopInScale());
        
        // Gravity-based physics animation
        while (elapsed < duration)
        {            
            float deltaTime = Time.deltaTime;
            elapsed += deltaTime;
            
            // Apply gravity physics
            currentVerticalVelocity += gravity * deltaTime;
            currentVerticalPosition += currentVerticalVelocity * deltaTime;
            horizontalPosition += horizontalVelocity * deltaTime;
            
            // Bounce if we hit the "ground" (below starting position)
            if (currentVerticalPosition < 0 && currentVerticalVelocity < 0)
            {
                currentVerticalPosition = 0f;
                currentVerticalVelocity = -currentVerticalVelocity * 0.6f; // Bounce with 60% energy
                horizontalVelocity *= 0.8f; // Reduce horizontal movement on bounce
                
                // Stop bouncing if the bounce is very small
                if (Mathf.Abs(currentVerticalVelocity) < 50f)
                {
                    currentVerticalVelocity = 0;
                    currentVerticalPosition = 0;
                }
            }
            
            // Apply the position with physics simulation
            comboText.transform.position = startPosition + new Vector3(
                horizontalPosition, 
                currentVerticalPosition, 
                0
            );
            
            // Add rotation based on vertical velocity for more dynamic feel
            float tiltAmount = Mathf.Clamp(-(currentVerticalVelocity / 400f), -10f, 10f);
            comboText.transform.rotation = Quaternion.Euler(0, 0, tiltAmount);
            
            yield return null;
        }
        
        // Return to the original position and rotation
        comboText.transform.position = startPosition;
        comboText.transform.rotation = Quaternion.identity;
    }
    
    // Quick pop-in scale effect
    private IEnumerator PopInScale()
    {
        // Initial pop effect
        float popDuration = 0.15f;
        float elapsed = 0f;
        
        // Pop to larger scale
        Vector3 largeScale = originalScale * popupScale;
        
        // Quick pop out to larger size
        while (elapsed < popDuration)
        {
            float t = elapsed / popDuration;
            // Ease out quad
            float easedT = t * (2 - t);
            comboText.transform.localScale = Vector3.Lerp(originalScale, largeScale, easedT);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Then slightly contract back
        elapsed = 0f;
        popDuration = 0.1f;
        
        while (elapsed < popDuration)
        {
            float t = elapsed / popDuration;
            comboText.transform.localScale = Vector3.Lerp(largeScale, originalScale * 1.1f, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Apply pulsing throughout the physics animation
        float pulseDuration = 1.0f;
        elapsed = 0f;
        
        while (elapsed < pulseDuration)
        {
            float t = elapsed / pulseDuration;
            float pulse = 1.1f + Mathf.Sin(t * Mathf.PI * 3) * 0.1f;
            comboText.transform.localScale = originalScale * pulse;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we return to original scale
        comboText.transform.localScale = originalScale;
    }
    
    /// <summary>
    /// Fades out the combo text after a specified delay
    /// </summary>
    private IEnumerator FadeOutComboTextAfterDelay()
    {
        // Wait for the display duration
        yield return new WaitForSeconds(displayDuration);
        
        // Only fade out if we still have the same combo count
        // (prevents fading if new combo has been shown since)
        if (comboText != null && rhythmFighter != null && rhythmFighter.comboCount == lastComboCount)
        {
            // Fade out over time
            float fadeDuration = 0.5f;
            float elapsed = 0f;
            Color originalColor = comboText.color;
            
            while (elapsed < fadeDuration)
            {
                float t = elapsed / fadeDuration;
                
                // Fade color
                Color fadeColor = originalColor;
                fadeColor.a = Mathf.Lerp(1, 0, t);
                comboText.color = fadeColor;
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Ensure text is invisible at the end
            Color finalColor = comboText.color;
            finalColor.a = 0;
            comboText.color = finalColor;
            
            // Clear the text
            comboText.text = "";
        }
    }
    
    // Call this when a perfect hit happens
    public void ShowPerfectHit()
    {
        if (comboText != null)
        {
            StartCoroutine(ShowPerfectHitText());
        }
    }
    
    private IEnumerator ShowPerfectHitText()
    {
        // Store current text
        string originalText = comboText.text;
        Color originalColor = comboText.color;
        
        // Show perfect text and change color
        comboText.text = "PERFECT!";
        comboText.color = Color.green;
        
        yield return new WaitForSeconds(0.5f);
        
        // Return to original text
        comboText.text = originalText;
        comboText.color = originalColor;
    }
    
    // Call this when a miss happens
    public void ShowMiss()
    {
        if (comboText != null)
        {
            StartCoroutine(ShowMissText());
        }
    }
    
    private IEnumerator ShowMissText()
    {
        // Store current text
        string originalText = comboText.text;
        Color originalColor = comboText.color;
        
        // Show miss text and change color
        comboText.text = "MISS!";
        comboText.color = Color.red;
        
        yield return new WaitForSeconds(0.5f);
        
        // Return to original text
        comboText.text = originalText;
        comboText.color = originalColor;
    }
}
