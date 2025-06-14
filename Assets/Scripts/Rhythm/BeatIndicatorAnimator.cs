using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the animation and visual feedback of the beat indicator.
/// </summary>
public class BeatIndicatorAnimator : MonoBehaviour
{
    // References
    public Image indicatorImage;
    
    // Animation settings
    public float pulseScale = 1.5f;
    public float pulseDuration = 0.2f;
    
    // Colors for different states
    public Color readyColor = Color.yellow;
    public Color perfectColor = Color.green;
    public Color missedColor = Color.red;
    
    // References to system
    private SimpleRhythmSystem rhythmSystem;
    private Vector3 originalScale;
    private Coroutine pulseCoroutine;
    private Coroutine colorCoroutine;
    
    private void Start()
    {
        // Cache the original scale
        originalScale = transform.localScale;
        
        // Find the rhythm system
        rhythmSystem = FindObjectOfType<SimpleRhythmSystem>();
        if (rhythmSystem != null)
        {
            // Subscribe to events
            rhythmSystem.OnBeat += OnBeat;
            
            // Set initial color
            indicatorImage.color = readyColor;
        }
        else
        {
            Debug.LogWarning("BeatIndicatorAnimator: No SimpleRhythmSystem found");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (rhythmSystem != null)
        {
            rhythmSystem.OnBeat -= OnBeat;
        }
    }
    
    private void Update()
    {
        if (rhythmSystem != null)
        {
            // Check if we're in the hit window to provide visual feedback
            // SimpleRhythmSystem.IsOnBeat() checks if we're in the hit window 
            bool inHitWindow = rhythmSystem.IsOnBeat();
            
            // If we're in the hit window, show feedback
            if (inHitWindow && indicatorImage.color == readyColor)
            {
                PulseIndicator();
            }
            
            // We can also check if we're near a beat by looking at the beat indicator scale
            // The system already animates this based on beat progress
            if (rhythmSystem.beatIndicator != null)
            {
                float currentScale = rhythmSystem.beatIndicator.transform.localScale.x;
                if (currentScale > 1.15f && indicatorImage.color == readyColor)
                {
                    // As the beat indicator grows, we're approaching a beat
                    PulseIndicator();
                }
            }
        }
    }
    
    private void OnBeat()
    {
        // Show perfect hit feedback
        ShowPerfectHit();
    }
    
    public void ShowPerfectHit()
    {
        // Change color to perfect and pulse
        ChangeColor(perfectColor);
        PulseIndicator();
    }
    
    public void ShowMissedBeat()
    {
        // Change color to missed
        ChangeColor(missedColor);
    }
    
    private void ChangeColor(Color targetColor)
    {
        // Stop any existing color change
        if (colorCoroutine != null)
        {
            StopCoroutine(colorCoroutine);
        }
        
        // Start a new color change
        colorCoroutine = StartCoroutine(ColorChangeRoutine(targetColor));
    }
    
    private IEnumerator ColorChangeRoutine(Color targetColor)
    {
        // Change to target color immediately
        indicatorImage.color = targetColor;
        
        // Wait a moment
        yield return new WaitForSeconds(0.5f);
        
        // Fade back to ready color
        float elapsed = 0f;
        float duration = 0.3f;
        Color startColor = indicatorImage.color;
        
        while (elapsed < duration)
        {
            indicatorImage.color = Color.Lerp(startColor, readyColor, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we end on the ready color
        indicatorImage.color = readyColor;
    }
    
    public void PulseIndicator()
    {
        // Stop any existing pulse
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }
        
        // Start a new pulse
        pulseCoroutine = StartCoroutine(PulseRoutine());
    }
    
    private IEnumerator PulseRoutine()
    {
        // Scale up
        float elapsed = 0f;
        float halfDuration = pulseDuration * 0.5f;
        
        while (elapsed < halfDuration)
        {
            float t = elapsed / halfDuration;
            transform.localScale = Vector3.Lerp(originalScale, originalScale * pulseScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Scale back down
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            float t = elapsed / halfDuration;
            transform.localScale = Vector3.Lerp(originalScale * pulseScale, originalScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we end at the original scale
        transform.localScale = originalScale;
    }
}
