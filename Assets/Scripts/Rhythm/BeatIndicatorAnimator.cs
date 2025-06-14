using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the animation and visual feedback of the beat indicator with juicy effects.
/// </summary>
public class BeatIndicatorAnimator : MonoBehaviour
{
    // References
    public Image indicatorImage;
    public GameObject glowOverlay; // Optional glow effect object
    public ParticleSystem hitParticles; // Optional particle system
    
    // Animation settings
    [Header("Pulse Settings")]
    public float pulseScale = 1.5f;
    public float pulseDuration = 0.2f;
    public AnimationCurve pulseEasingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool useElasticScale = true;
    
    [Header("Rotation Settings")]
    public bool useRotationEffect = true;
    public float rotationSpeed = 20f;
    public float rotationOnHit = 30f;
    
    [Header("Colors")]
    public Color readyColor = new Color(1f, 0.8f, 0.2f, 1f); // More vibrant yellow
    public Color perfectColor = new Color(0.3f, 1f, 0.5f, 1f); // More vibrant green
    public Color missedColor = new Color(1f, 0.3f, 0.3f, 1f); // More vibrant red
    public bool useGlowEffect = true;
    
    [Header("Visual Effects")]
    public bool useTrailEffect = true;
    public int maxTrailSprites = 3;
    public float trailOpacity = 0.3f;
    
    // References to system
    private SimpleRhythmSystem rhythmSystem;
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private List<GameObject> trailObjects = new List<GameObject>();
    private Coroutine pulseCoroutine;
    private Coroutine colorCoroutine;
    private Coroutine rotateCoroutine;
    private Coroutine trailCoroutine;
    
    private void Start()
    {
        // Cache the original scale and rotation
        originalScale = transform.localScale;
        originalRotation = transform.rotation;
        
        // Find the rhythm system
        rhythmSystem = FindObjectOfType<SimpleRhythmSystem>();
        if (rhythmSystem != null)
        {
            // Subscribe to events
            rhythmSystem.OnBeat += OnBeat;
            
            // Set initial color
            indicatorImage.color = readyColor;
            
            // Create trail objects if enabled
            if (useTrailEffect)
            {
                CreateTrailObjects();
            }
            
            // Start continuous rotation if enabled
            if (useRotationEffect)
            {
                rotateCoroutine = StartCoroutine(ContinuousRotationRoutine());
            }
        }
        else
        {
            Debug.LogWarning("BeatIndicatorAnimator: No SimpleRhythmSystem found");
        }
        
        // Initialize optional components
        if (glowOverlay != null)
        {
            Image glowImage = glowOverlay.GetComponent<Image>();
            if (glowImage != null)
            {
                // Set initial glow color with reduced alpha
                Color glowColor = readyColor;
                glowColor.a *= 0.5f;
                glowImage.color = glowColor;
            }
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (rhythmSystem != null)
        {
            rhythmSystem.OnBeat -= OnBeat;
        }
        
        // Stop all coroutines
        StopAllCoroutines();
        
        // Clean up trail objects
        foreach (var trail in trailObjects)
        {
            if (trail != null)
            {
                Destroy(trail);
            }
        }
        trailObjects.Clear();
    }
    
    private void Update()
    {
        if (rhythmSystem != null)
        {
            // Check if we're in the hit window to provide visual feedback
            bool inHitWindow = rhythmSystem.IsOnBeat();
            
            // If we're in the hit window, show feedback
            if (inHitWindow && indicatorImage.color == readyColor)
            {
                PulseIndicator();
                
                // Update trail visibility
                if (useTrailEffect && trailCoroutine == null)
                {
                    trailCoroutine = StartCoroutine(TrailEffectRoutine());
                }
            }
            
            // Subtle breathing animation when idle
            if (pulseCoroutine == null && rotateCoroutine == null && !inHitWindow)
            {
                float breathingScale = 1.0f + 0.05f * Mathf.Sin(Time.time * 2f);
                transform.localScale = originalScale * breathingScale;
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
        // Change color to perfect and pulse with extra effects
        ChangeColor(perfectColor);
        PulseIndicator(1.8f); // Larger pulse for perfect hits
        
        // Trigger special visual effects
        if (hitParticles != null)
        {
            hitParticles.Play();
        }
        
        // Quick rotation burst if enabled
        if (useRotationEffect)
        {
            StartCoroutine(RotationBurstRoutine(rotationOnHit));
        }
        
        // Enhance glow effect if available
        if (glowOverlay != null && useGlowEffect)
        {
            Image glowImage = glowOverlay.GetComponent<Image>();
            if (glowImage != null)
            {
                StartCoroutine(GlowEffectRoutine(perfectColor));
            }
        }
    }
    
    public void ShowMissedBeat()
    {
        // Change color to missed and do a small pulse
        ChangeColor(missedColor);
        PulseIndicator(0.9f); // Smaller pulse for misses
        
        // Shake briefly to indicate miss
        StartCoroutine(ShakeRoutine(0.2f, 0.05f));
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
        // Change to target color immediately with flash effect
        indicatorImage.color = Color.white; // Flash white first
        yield return new WaitForSeconds(0.05f);
        indicatorImage.color = targetColor;
        
        // Update glow if available
        if (glowOverlay != null && useGlowEffect)
        {
            Image glowImage = glowOverlay.GetComponent<Image>();
            if (glowImage != null)
            {
                Color glowColor = targetColor;
                glowColor.a *= 0.7f;
                glowImage.color = glowColor;
            }
        }
        
        // Wait a moment
        yield return new WaitForSeconds(0.5f);
        
        // Fade back to ready color
        float elapsed = 0f;
        float duration = 0.5f;
        Color startColor = indicatorImage.color;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // Use smoothstep for nicer transition
            t = t * t * (3f - 2f * t);
            indicatorImage.color = Color.Lerp(startColor, readyColor, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we end on the ready color
        indicatorImage.color = readyColor;
        
        // Reset glow color
        if (glowOverlay != null && useGlowEffect)
        {
            Image glowImage = glowOverlay.GetComponent<Image>();
            if (glowImage != null)
            {
                Color glowColor = readyColor;
                glowColor.a *= 0.5f;
                glowImage.color = glowColor;
            }
        }
    }
    
    public void PulseIndicator(float intensityMultiplier = 1.0f)
    {
        // Stop any existing pulse
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }
        
        // Start a new pulse with optional intensity multiplier
        pulseCoroutine = StartCoroutine(PulseRoutine(intensityMultiplier));
    }
    
    private IEnumerator PulseRoutine(float intensityMultiplier = 1.0f)
    {
        // Calculate adjusted scale based on intensity
        float adjustedPulseScale = pulseScale * intensityMultiplier;
        
        // Scale up with elastic overshoot if enabled
        float elapsed = 0f;
        float halfDuration = pulseDuration * 0.5f;
        
        while (elapsed < halfDuration)
        {
            float t = elapsed / halfDuration;
            
            // Apply easing curve for juicy effect
            if (pulseEasingCurve != null)
            {
                t = pulseEasingCurve.Evaluate(t);
            }
            
            // Apply elastic overshoot if enabled
            if (useElasticScale && intensityMultiplier > 1.0f)
            {
                // Add elastic overshoot with oscillation
                float elasticFactor = 1.0f + Mathf.Sin(t * Mathf.PI * 3) * 0.1f * intensityMultiplier;
                transform.localScale = Vector3.Lerp(originalScale, originalScale * adjustedPulseScale * elasticFactor, t);
            }
            else
            {
                transform.localScale = Vector3.Lerp(originalScale, originalScale * adjustedPulseScale, t);
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Scale back down with slight bounce
        elapsed = 0f;
        float scalePeak = transform.localScale.x; // Current scale at peak
        Vector3 peakScale = transform.localScale;
        
        while (elapsed < halfDuration * 1.2f) // Slightly longer return for natural feel
        {
            float t = elapsed / (halfDuration * 1.2f);
            
            // Apply easing curve with extra bounce
            if (useElasticScale)
            {
                // Add bounce effect
                float bounce = 1.0f - Mathf.Abs(Mathf.Sin(t * Mathf.PI * (intensityMultiplier > 1.0f ? 2 : 1))) * 0.05f * intensityMultiplier;
                transform.localScale = Vector3.Lerp(peakScale, originalScale * bounce, t);
            }
            else
            {
                // Simple ease back to original
                t = t * t * (3f - 2f * t); // Smoothstep
                transform.localScale = Vector3.Lerp(peakScale, originalScale, t);
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we end at the original scale
        transform.localScale = originalScale;
        pulseCoroutine = null;
    }
    
    // Creates trail objects for the echo effect
    private void CreateTrailObjects()
    {
        // Clear existing trails first
        foreach (var trail in trailObjects)
        {
            if (trail != null)
            {
                Destroy(trail);
            }
        }
        trailObjects.Clear();
        
        // Create new trail objects
        for (int i = 0; i < maxTrailSprites; i++)
        {
            GameObject trailObj = new GameObject("Trail_" + i);
            trailObj.transform.SetParent(transform.parent);
            trailObj.transform.localPosition = transform.localPosition;
            trailObj.transform.localScale = transform.localScale * 0.9f;
            trailObj.transform.localRotation = transform.localRotation;
            
            // Add image component
            Image trailImage = trailObj.AddComponent<Image>();
            if (indicatorImage != null)
            {
                trailImage.sprite = indicatorImage.sprite;
                Color trailColor = indicatorImage.color;
                trailColor.a *= trailOpacity * ((float)(maxTrailSprites - i) / maxTrailSprites);
                trailImage.color = trailColor;
            }
            
            // Disable initially
            trailObj.SetActive(false);
            trailObjects.Add(trailObj);
        }
    }
    
    // Trail effect animation
    private IEnumerator TrailEffectRoutine()
    {
        if (trailObjects.Count == 0)
        {
            CreateTrailObjects();
        }
        
        // Activate and animate trails
        for (int i = 0; i < trailObjects.Count; i++)
        {
            if (trailObjects[i] != null)
            {
                trailObjects[i].SetActive(true);
            }
        }
        
        float duration = 0.5f;
        float elapsed = 0f;
        
        // Animate trails to fade out
        while (elapsed < duration)
        {
            // Update trail positions with delay
            for (int i = 0; i < trailObjects.Count; i++)
            {
                if (trailObjects[i] != null)
                {
                    // Calculate delay based on trail index
                    float delay = (float)i / (trailObjects.Count) * 0.8f;
                    float progress = Mathf.Clamp01((elapsed - delay) / 0.3f);
                    
                    // If enough time has passed for this trail
                    if (progress > 0)
                    {
                        // Move towards current position
                        trailObjects[i].transform.position = Vector3.Lerp(
                            trailObjects[i].transform.position,
                            transform.position,
                            0.2f * (1f - ((float)i / trailObjects.Count))
                        );
                        
                        // Fade out and scale down
                        Image trailImage = trailObjects[i].GetComponent<Image>();
                        if (trailImage != null)
                        {
                            Color color = trailImage.color;
                            color.a = trailOpacity * (1f - progress) * ((float)(trailObjects.Count - i) / trailObjects.Count);
                            trailImage.color = color;
                            
                            trailObjects[i].transform.localScale = transform.localScale * (0.9f - progress * 0.3f);
                        }
                    }
                }
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Hide all trails
        foreach (var trail in trailObjects)
        {
            if (trail != null)
            {
                trail.SetActive(false);
            }
        }
        
        trailCoroutine = null;
    }
    
    // Rotation effect
    private IEnumerator ContinuousRotationRoutine()
    {
        while (true)
        {
            transform.Rotate(new Vector3(0, 0, rotationSpeed * Time.deltaTime));
            yield return null;
        }
    }
    
    // Rotation burst effect for hits
    private IEnumerator RotationBurstRoutine(float rotationAmount)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = startRotation * Quaternion.Euler(0, 0, rotationAmount);
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
    
    // Glow overlay effect
    private IEnumerator GlowEffectRoutine(Color targetColor)
    {
        if (glowOverlay == null) yield break;
        
        Image glowImage = glowOverlay.GetComponent<Image>();
        if (glowImage == null) yield break;
        
        // Initial scale and color 
        glowOverlay.transform.localScale = transform.localScale * 1.2f;
        
        // Brighter glow for effect
        Color glowColor = targetColor;
        glowColor.a = 0.9f;
        glowImage.color = glowColor;
        
        // Expand glow
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 startScale = glowOverlay.transform.localScale;
        Vector3 endScale = startScale * 1.5f;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t); // Smoothstep
            
            // Scale
            glowOverlay.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            
            // Fade
            Color currentColor = glowImage.color;
            currentColor.a = Mathf.Lerp(0.9f, 0.1f, t);
            glowImage.color = currentColor;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Reset
        glowOverlay.transform.localScale = startScale;
        glowColor.a = 0.5f;
        glowImage.color = glowColor;
    }
    
    // Shake effect for misses
    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            
            transform.localPosition = new Vector3(
                originalPos.x + x,
                originalPos.y + y,
                originalPos.z
            );
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localPosition = originalPos;
    }
}
