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
        // Pop-up animation
        float elapsed = 0;
        
        // Scale up quickly
        while (elapsed < popupDuration * 0.3f)
        {
            float t = elapsed / (popupDuration * 0.3f);
            comboText.transform.localScale = Vector3.Lerp(originalScale, originalScale * popupScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Scale back down more slowly
        elapsed = 0;
        Vector3 currentScale = comboText.transform.localScale;
        
        while (elapsed < popupDuration * 0.7f)
        {
            float t = elapsed / (popupDuration * 0.7f);
            comboText.transform.localScale = Vector3.Lerp(currentScale, originalScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we end at the original scale
        comboText.transform.localScale = originalScale;
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
