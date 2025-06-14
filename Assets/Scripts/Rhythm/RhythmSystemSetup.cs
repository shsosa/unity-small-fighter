using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Setup for the SimpleRhythm system with inspector configuration.
/// Add this to any GameObject in your scene to initialize the rhythm system.
/// </summary>
// Helper class to maintain connection references
public class RhythmShakeConnection {
    public SimpleRhythmSystem rhythmSystem;
    public ScreenShakeManager shakeManager;
}

public class RhythmSystemSetup : MonoBehaviour
{
    [Header("Music Settings")]
    public AudioClip musicClip;
    public float bpm = 120f;
    public float volume = 0.7f;
    
    [Header("Rhythm Settings")]
    public float beatWindowSeconds = 0.15f;
    public float onBeatDamageMultiplier = 1.5f;
    public float maxComboMultiplier = 2.0f;
    public float comboMultiplierIncrement = 0.1f;
    
    [Header("Visual Settings")]
    public Color beatIndicatorColor = new Color(1, 0.8f, 0, 0.8f); // Golden yellow
    public Color perfectTimingColor = Color.green;
    public Color missedTimingColor = Color.red;
    
    [Header("UI Feedback")]
    public bool createFeedbackUI = true;
    public bool showComboText = true;
    public Vector2 comboTextPosition = new Vector2(0, 200);
    public int comboTextFontSize = 48;
    public float comboPopupDuration = 0.5f;
    public float comboPopupScale = 1.5f;
    
    [Header("Beat Indicator")]
    public float beatIndicatorSize = 1.0f;
    public float beatIndicatorPulseScale = 1.5f;
    public float beatIndicatorPulseDuration = 0.2f;
    
    [Header("Screen Shake")]
    public bool enableScreenShake = true;
    public float shakeIntensity = 0.1f;
    public float shakeDuration = 0.2f;
    
    // Reference list to maintain connections
    private static List<RhythmShakeConnection> rhythmManagersInScene = new List<RhythmShakeConnection>();
    
    private void Start()
    {
        // Create the rhythm system GameObject
        GameObject rhythmSystemObj = new GameObject("SimpleRhythmSystem");
        SimpleRhythmSystem rhythmSystem = rhythmSystemObj.AddComponent<SimpleRhythmSystem>();
        
        // Configure rhythm settings from inspector
        rhythmSystem.bpm = bpm;
        rhythmSystem.beatWindowSeconds = beatWindowSeconds;
        rhythmSystem.onBeatDamageMultiplier = onBeatDamageMultiplier;
        rhythmSystem.maxComboMultiplier = maxComboMultiplier;
        rhythmSystem.comboMultiplierIncrement = comboMultiplierIncrement;
        
        // Configure colors if available
        if (rhythmSystem.readyColor != null)
            rhythmSystem.readyColor = beatIndicatorColor;
        if (rhythmSystem.perfectColor != null)
            rhythmSystem.perfectColor = perfectTimingColor;
        if (rhythmSystem.missedColor != null)
            rhythmSystem.missedColor = missedTimingColor;
        
        // Set up audio source
        AudioSource musicSource = rhythmSystemObj.AddComponent<AudioSource>();
        rhythmSystem.musicSource = musicSource;
        
        // Use music clip from inspector
        if (musicClip != null)
        {
            musicSource.clip = musicClip;
            musicSource.loop = true;
            musicSource.volume = volume;
            Debug.Log("RhythmSystemSetup: Using music clip " + musicClip.name);
        }
        else
        {
            Debug.LogWarning("RhythmSystemSetup: No music clip assigned in inspector");
            // Create a simple beat as fallback
            // The system will create one automatically
        }
        
        // Create enhanced UI elements
        if (createFeedbackUI)
        {
            StartCoroutine(SetupEnhancedUI(rhythmSystem));
        }
        // Otherwise the SimpleRhythmSystem will create basic UI elements by default
        
        // Find all fighters and add rhythm components to them
        StartCoroutine(SetupFightersDelayed());
        
        Debug.Log("RhythmSystemSetup: Rhythm system initialized");
    }
    
    private IEnumerator SetupFightersDelayed()
    {
        // Wait a frame to ensure all fighters are loaded
        yield return null;
        
        // Find all fighters in the scene
        NewFighter[] fighters = FindObjectsOfType<NewFighter>();
        
        if (fighters.Length == 0)
        {
            Debug.LogWarning("RhythmSystemSetup: No fighters found in the scene");
            yield break;
        }
        
        // Create a hit effect prefab for visual feedback
        GameObject hitEffectPrefab = CreateHitEffectPrefab();
        
        // Add rhythm components to all fighters
        foreach (NewFighter fighter in fighters)
        {
            // Skip if already has rhythm component
            SimpleRhythmFighter rhythmFighter = fighter.GetComponent<SimpleRhythmFighter>();
            if (rhythmFighter != null)
                continue;
                
            // Add rhythm fighter component
            rhythmFighter = fighter.gameObject.AddComponent<SimpleRhythmFighter>();
            rhythmFighter.fighter = fighter;
            rhythmFighter.onBeatDamageMultiplier = onBeatDamageMultiplier;
            rhythmFighter.maxComboMultiplier = maxComboMultiplier;
            rhythmFighter.comboMultiplierIncrement = comboMultiplierIncrement;
            rhythmFighter.hitEffectPrefab = hitEffectPrefab;
            
            Debug.Log($"RhythmSystemSetup: Added SimpleRhythmFighter to {fighter.name}");
        }
    }
    
    private IEnumerator SetupEnhancedUI(SimpleRhythmSystem rhythmSystem)
    {
        yield return null; // Wait a frame to ensure the rhythm system is fully initialized
        
        // Find or create a canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("RhythmCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create combo text UI
        if (showComboText)
        {
            CreateComboFeedbackUI(canvas, rhythmSystem);
        }
        
        // Create beat indicator
        CreateBeatIndicatorUI(canvas, rhythmSystem);
        
        // Connect to SimpleRhythmSystem events
        ConnectToRhythmEvents(rhythmSystem);
        
        Debug.Log("RhythmSystemSetup: Enhanced UI setup complete");
    }
    
    private void CreateComboFeedbackUI(Canvas canvas, SimpleRhythmSystem rhythmSystem)
    {
        // Find all fighters that have rhythm component
        SimpleRhythmFighter[] rhythmFighters = FindObjectsOfType<SimpleRhythmFighter>();
        float offset = 200f;
        
        // Create a mapping dictionary to track which fighter has which text
        Dictionary<SimpleRhythmFighter, TextMeshProUGUI> comboTexts = 
            new Dictionary<SimpleRhythmFighter, TextMeshProUGUI>();
        
        // Create UI first, then assign to fighters after all UI elements are created
        for (int i = 0; i < rhythmFighters.Length; i++)
        {
            // Create combo text container
            GameObject comboObj = new GameObject($"ComboText_Player{i+1}");
            comboObj.transform.SetParent(canvas.transform, false);
            
            // Create the combo counter text
            TextMeshProUGUI comboText = comboObj.AddComponent<TextMeshProUGUI>();
            comboText.text = "";
            comboText.fontSize = comboTextFontSize;
            comboText.fontStyle = FontStyles.Bold;
            comboText.alignment = TextAlignmentOptions.Center;
            comboText.color = perfectTimingColor;
            
            // Position the text - left or right side based on player number
            RectTransform rectTransform = comboText.GetComponent<RectTransform>();
            float xPos = (i == 0) ? -offset : offset;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(xPos, comboTextPosition.y);
            rectTransform.sizeDelta = new Vector2(300, 100);
            
            // Store in our mapping dictionary
            comboTexts[rhythmFighters[i]] = comboText;
        }
        
        // Now iterate through all rhythm fighters and assign the text component
        foreach (var pair in comboTexts)
        {
            SimpleRhythmFighter fighter = pair.Key;
            TextMeshProUGUI textComponent = pair.Value;
            
            // Store reference to fighter for combo tracking
            fighter.comboText = textComponent;
        }
    }
    
    private void CreateBeatIndicatorUI(Canvas canvas, SimpleRhythmSystem rhythmSystem)
    {
        // Create beat indicator game object
        GameObject beatIndicatorObj = new GameObject("BeatIndicator");
        beatIndicatorObj.transform.SetParent(canvas.transform, false);
        
        // Add an image component for the beat indicator
        Image indicatorImage = beatIndicatorObj.AddComponent<Image>();
        indicatorImage.sprite = CreateCircleSprite(); // Same sprite we use for hit effects
        indicatorImage.color = beatIndicatorColor;
        
        // Position it at the bottom center of the screen
        RectTransform indicatorRect = indicatorImage.GetComponent<RectTransform>();
        indicatorRect.anchorMin = new Vector2(0.5f, 0);
        indicatorRect.anchorMax = new Vector2(0.5f, 0);
        indicatorRect.pivot = new Vector2(0.5f, 0.5f);
        indicatorRect.anchoredPosition = new Vector2(0, 100);
        indicatorRect.sizeDelta = new Vector2(80, 80);
        
        // Set reference in rhythm system
        rhythmSystem.beatIndicator = beatIndicatorObj;
        
        // Add a beat indicator animator component to handle color and size changes
        BeatIndicatorAnimator animator = beatIndicatorObj.AddComponent<BeatIndicatorAnimator>();
        animator.indicatorImage = indicatorImage;
        animator.pulseScale = beatIndicatorPulseScale;
        animator.pulseDuration = beatIndicatorPulseDuration;
        animator.readyColor = beatIndicatorColor;
        animator.perfectColor = perfectTimingColor;
        animator.missedColor = missedTimingColor;
        
        // Add screen shake component
        if (enableScreenShake)
        {
            ScreenShakeManager shakeManager = FindOrCreateScreenShakeManager(canvas);
            // Store reference for direct access
            rhythmSystem.GetComponent<MonoBehaviour>().StartCoroutine(MonitorBeats(rhythmSystem, shakeManager));
            
            // Save a persistent reference to avoid garbage collection
            rhythmManagersInScene.Add(new RhythmShakeConnection { 
                rhythmSystem = rhythmSystem, 
                shakeManager = shakeManager 
            });
        }
        
        // Create a hidden stub for compatibility with original system
        // (SimpleRhythmSystem might expect these to not be null)
        SetUpCompatibilityStubs(canvas, rhythmSystem);
    }
    
    private void SetUpCompatibilityStubs(Canvas canvas, SimpleRhythmSystem rhythmSystem)
    {
        // Create an invisible timing bar for compatibility
        GameObject hiddenBar = new GameObject("HiddenTimingBar");
        hiddenBar.transform.SetParent(canvas.transform, false);
        Image barImage = hiddenBar.AddComponent<Image>();
        barImage.color = Color.clear;
        RectTransform barRect = barImage.GetComponent<RectTransform>();
        barRect.sizeDelta = new Vector2(1, 1);
        barRect.anchoredPosition = new Vector2(-10000, -10000);
        rhythmSystem.hitTimingBar = barImage;
        
        // Create hidden text for compatibility
        GameObject hiddenTextObj = new GameObject("HiddenText");
        hiddenTextObj.transform.SetParent(canvas.transform, false);
        Text hiddenText = hiddenTextObj.AddComponent<Text>();
        hiddenText.color = Color.clear;
        RectTransform textRect = hiddenText.GetComponent<RectTransform>();
        textRect.anchoredPosition = new Vector2(-10000, -10000);
        rhythmSystem.hitNowText = hiddenText;
    }
    
    private ScreenShakeManager FindOrCreateScreenShakeManager(Canvas canvas)
    {
        // Find existing or create new screen shake manager
        ScreenShakeManager shaker = FindObjectOfType<ScreenShakeManager>();
        if (shaker == null)
        {
            GameObject shakerObj = new GameObject("ScreenShakeManager");
            shaker = shakerObj.AddComponent<ScreenShakeManager>();
            shaker.targetCanvas = canvas;
        }
        return shaker;
    }
    
    // Monitor beats and trigger effects
    private IEnumerator MonitorBeats(SimpleRhythmSystem rhythmSystem, ScreenShakeManager shakeManager)
    {
        BeatIndicatorAnimator beatAnimator = null;
        
        if (rhythmSystem.beatIndicator != null) {
            beatAnimator = rhythmSystem.beatIndicator.GetComponent<BeatIndicatorAnimator>();
        }
        
        float lastCheckTime = Time.time;
        bool wasOnBeat = false;
        
        while (rhythmSystem != null && rhythmSystem.gameObject != null)
        {
            // Check for beat
            bool isOnBeat = rhythmSystem.IsOnBeat();
            
            // Detect new beat
            if (isOnBeat && !wasOnBeat)
            {
                // Update beat animator if available (but don't shake)
                if (beatAnimator != null)
                {
                    // Just pulse the beat indicator, don't change color
                    beatAnimator.PulseIndicator();
                }
            }
            else if (!isOnBeat && wasOnBeat)
            {
                // End of beat - could show missed here
            }
            
            wasOnBeat = isOnBeat;
            
            yield return null;
        }
    }
    
    private void ConnectToRhythmEvents(SimpleRhythmSystem rhythmSystem)
    {
        // Connect rhythm fighters to the beat indicator for color feedback
        SimpleRhythmFighter[] fighters = FindObjectsOfType<SimpleRhythmFighter>();
        BeatIndicatorAnimator beatAnimator = null;
        
        if (rhythmSystem.beatIndicator != null) {
            beatAnimator = rhythmSystem.beatIndicator.GetComponent<BeatIndicatorAnimator>();
        }
        
        foreach (SimpleRhythmFighter fighter in fighters)
        {
            // Enable combo text animation
            EnableComboAnimation(fighter);
            
            // Connect to OnHit and OnMiss events with captured references
            if (beatAnimator != null) {
                SimpleRhythmFighter capturedFighter = fighter;
                BeatIndicatorAnimator capturedAnimator = beatAnimator;
                
                // Hook up perfect hit feedback
                fighter.OnPerfectHit += () => {
                    capturedAnimator.ShowPerfectHit();
                    
                    // Directly trigger screen shake on perfect hit using static method
                    if (enableScreenShake)
                    {
                        ScreenShakeManager.ShakeScreen(shakeDuration, shakeIntensity);
                    }
                };
                
                // Hook up missed feedback
                fighter.OnMissedBeat += () => {
                    capturedAnimator.ShowMissedBeat();
                };
            }
        }
    }
    
    private void EnableComboAnimation(SimpleRhythmFighter fighter)
    {
        // Add MonoBehaviour to handle animations
        ComboTextAnimator animator = fighter.gameObject.AddComponent<ComboTextAnimator>();
        animator.comboText = fighter.comboText;
        animator.popupDuration = comboPopupDuration;
        animator.popupScale = comboPopupScale;
    }

    private GameObject CreateHitEffectPrefab()
    {
        GameObject prefab = new GameObject("RhythmHitEffect");
        
        // Add sprite renderer with a circle sprite
        SpriteRenderer renderer = prefab.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateCircleSprite();
        renderer.color = beatIndicatorColor;
        
        // Add animation script
        SimpleHitEffect hitEffect = prefab.AddComponent<SimpleHitEffect>();
        hitEffect.duration = 0.5f; // Can be exposed to inspector if needed
        hitEffect.growSpeed = 2.0f;
        
        // Hide and persist the prefab
        prefab.SetActive(false);
        DontDestroyOnLoad(prefab);
        
        return prefab;
    }
    
    private Sprite CreateCircleSprite()
    {
        int resolution = 128;
        Texture2D texture = new Texture2D(resolution, resolution);
        Color[] colors = new Color[resolution * resolution];
        
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dx = x - resolution / 2;
                float dy = y - resolution / 2;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                
                if (distance < resolution / 2)
                {
                    colors[y * resolution + x] = Color.white;
                }
                else
                {
                    colors[y * resolution + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
    }
}
