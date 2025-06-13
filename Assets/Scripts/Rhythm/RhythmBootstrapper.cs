using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AudioVisualizer;
using UnityEngine.UI;

/// <summary>
/// Automatically sets up the complete Rhythm Combat system with no manual setup required
/// Add this to any GameObject in your scene to instantly get a working rhythm fighting system
/// </summary>
public class RhythmBootstrapper : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip musicClip; // Optional: assign a music clip in inspector
    public string musicFilePath = "Sounds/bgm1"; // Will load this if musicClip is null
    
    void Awake()
    {
        Debug.Log("RhythmBootstrapper: Starting setup");
        StartCoroutine(SetupSystem());
    }
    
    IEnumerator SetupSystem()
    {
        // Wait one frame to ensure all components are initialized
        yield return null;
        
        // Step 1: Set up AudioSampler first
        SetupAudioSampler();
        
        // Step 2: Set up RhythmManager
        RhythmManager rhythmManager = SetupRhythmManager();
        
        // Step 3: Set up Beat Visualization
        SetupBeatVisualization(rhythmManager);
        
        // Step 4: Set up Rhythm Combat for all fighters
        SetupRhythmCombat();
        
        Debug.Log("RhythmBootstrapper: Setup complete!");
    }
    
    private AudioSampler SetupAudioSampler()
    {
        // Check if AudioSampler already exists
        AudioSampler audioSampler = FindObjectOfType<AudioSampler>();
        
        // Create AudioSampler if it doesn't exist
        if (audioSampler == null)
        {
            Debug.Log("RhythmBootstrapper: Creating AudioSampler");
            GameObject samplerObj = new GameObject("AudioSampler");
            audioSampler = samplerObj.AddComponent<AudioSampler>();
            
            // Music source
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(samplerObj.transform);
            AudioSource musicSource = musicObj.AddComponent<AudioSource>();
            
            // Try to load music clip
            if (musicClip == null)
            {
                musicClip = Resources.Load<AudioClip>(musicFilePath);
                
                // If still null, create a simple beep sound
                if (musicClip == null)
                {
                    Debug.LogWarning("RhythmBootstrapper: No music clip found, creating a simple beat");
                    musicClip = CreateSimpleBeatAudio();
                }
            }
            
            musicSource.clip = musicClip;
            musicSource.loop = true;
            musicSource.volume = 0.7f;
            musicSource.Play();
            
            // Configure AudioSampler
            audioSampler.audioSources = new List<AudioSource> { musicSource };
            audioSampler.debug = true;
            audioSampler.playOnAwake = true;
            
            // Make persistent
            DontDestroyOnLoad(samplerObj);
        }
        
        return audioSampler;
    }
    
    private AudioClip CreateSimpleBeatAudio()
    {
        // Create a simple beat pattern
        int sampleRate = 44100;
        int clipLength = 4; // 4 seconds
        AudioClip clip = AudioClip.Create("SimpleBeat", sampleRate * clipLength, 1, sampleRate, false);
        
        float[] samples = new float[sampleRate * clipLength];
        
        // Create a simple beat pattern (every 0.5 seconds)
        for (int i = 0; i < clipLength * 2; i++)
        {
            int beatStart = (int)(i * 0.5f * sampleRate);
            // Create a drum-like sound
            for (int j = 0; j < 3000; j++)
            {
                if (beatStart + j < samples.Length)
                {
                    samples[beatStart + j] = Mathf.Sin(j * 0.2f) * (1.0f - j / 3000f);
                }
            }
        }
        
        clip.SetData(samples, 0);
        return clip;
    }
    
    private RhythmManager SetupRhythmManager()
    {
        // Check if RhythmManager already exists
        RhythmManager rhythmManager = FindObjectOfType<RhythmManager>();
        
        // Create RhythmManager if it doesn't exist
        if (rhythmManager == null)
        {
            Debug.Log("RhythmBootstrapper: Creating RhythmManager");
            GameObject managerObj = new GameObject("RhythmManager");
            rhythmManager = managerObj.AddComponent<RhythmManager>();
            
            // Get the AudioSource that's being sampled by AudioSampler
            AudioSampler sampler = FindObjectOfType<AudioSampler>();
            if (sampler != null && sampler.audioSources.Count > 0)
            {
                rhythmManager.musicSource = sampler.audioSources[0];
            }
            else
            {
                // Create our own music source if none exists
                AudioSource musicSource = managerObj.AddComponent<AudioSource>();
                musicSource.clip = musicClip;
                musicSource.loop = true;
                musicSource.Play();
                rhythmManager.musicSource = musicSource;
            }
            
            // Configure rhythm manager
            rhythmManager.rhythmWindowSeconds = 0.15f;
            rhythmManager.onBeatDamageMultiplier = 1.5f;
            
            // Add an audio event listener
            AudioEventListener listener = managerObj.AddComponent<AudioEventListener>();
            listener.frequencyRange = FrequencyRange.LowMidrange;
            listener.beatThreshold = 1.2f;
            listener.automaticThreshold = true;
            listener.sampleBufferSize = 60;
            listener.debug = true;
            
            // Make persistent
            DontDestroyOnLoad(managerObj);
        }
        
        Debug.Log("RhythmBootstrapper: RhythmManager set up with music source");
        return rhythmManager;
    }
    
    private void SetupBeatVisualization(RhythmManager rhythmManager)
    {
        // Create UI canvas for visualization
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.Log("RhythmBootstrapper: Creating Canvas for beat visualization");
            GameObject canvasObj = new GameObject("RhythmCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Add debugging text
            GameObject textObj = new GameObject("InfoText");
            textObj.transform.SetParent(canvas.transform, false);
            Text text = textObj.AddComponent<Text>();
            Font arialFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
            text.font = arialFont;
            text.text = "Rhythm Combat Active";
            text.fontSize = 20;
            text.color = Color.yellow;
            text.alignment = TextAnchor.UpperLeft;
            
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 1);
            textRect.anchorMax = new Vector2(0, 1);
            textRect.pivot = new Vector2(0, 1);
            textRect.anchoredPosition = new Vector2(10, -10);
            textRect.sizeDelta = new Vector2(200, 30);
        }
        
        // Create beat indicators
        GameObject indicatorObj = CreateBeatIndicator(canvas.transform);
        rhythmManager.beatIndicatorPrefab = indicatorObj;
        rhythmManager.beatIndicatorParent = canvas.transform;
        
        Debug.Log("RhythmBootstrapper: Beat visualization set up");
    }
    
    private GameObject CreateBeatIndicator(Transform parent)
    {
        // Create beat indicator object
        GameObject indicatorObj = new GameObject("BeatIndicator");
        indicatorObj.transform.SetParent(parent, false);
        
        // Add image component
        Image image = indicatorObj.AddComponent<Image>();
        image.color = new Color(1, 1, 0, 0.8f); // Yellow
        
        // Create circular sprite
        Sprite sprite = CreateCircleSprite();
        image.sprite = sprite;
        
        // Set position and size
        RectTransform rect = indicatorObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0);
        rect.anchorMax = new Vector2(0.5f, 0);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, 80);
        rect.sizeDelta = new Vector2(40, 40);
        
        // Add beat indicator component
        RhythmBeatIndicator beatIndicator = indicatorObj.AddComponent<RhythmBeatIndicator>();
        
        return indicatorObj;
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
                // Calculate distance from center
                float dx = x - resolution / 2;
                float dy = y - resolution / 2;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                
                // Create circle
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
    
    private void SetupRhythmCombat()
    {
        // Find all fighters in the scene
        NewFighter[] fighters = FindObjectsOfType<NewFighter>();
        if (fighters.Length == 0)
        {
            Debug.LogWarning("RhythmBootstrapper: No fighters found in scene");
            return;
        }
        
        Debug.Log($"RhythmBootstrapper: Found {fighters.Length} fighters, adding rhythm combat extensions");
        
        foreach (NewFighter fighter in fighters)
        {
            // Add RhythmCombatExtension if not already added
            RhythmCombatExtension rhythmExt = fighter.GetComponent<RhythmCombatExtension>();
            if (rhythmExt == null)
            {
                rhythmExt = fighter.gameObject.AddComponent<RhythmCombatExtension>();
                rhythmExt.fighter = fighter;
                rhythmExt.maxComboMultiplier = 2.0f;
                rhythmExt.comboMultiplierIncrement = 0.2f;
                rhythmExt.onBeatDamageMultiplier = 1.5f;
                
                // Create hit effect prefab
                rhythmExt.onBeatHitEffectPrefab = CreateHitEffectPrefab();
                
                Debug.Log($"RhythmBootstrapper: Added RhythmCombatExtension to {fighter.gameObject.name}");
            }
        }
        
        // Update FightManager to show rhythm hit messages
        FightManager fightManager = FindObjectOfType<FightManager>();
        if (fightManager != null)
        {
            fightManager.rhythmHitTextPrefab = CreateRhythmHitTextPrefab();
            Debug.Log("RhythmBootstrapper: Connected to FightManager");
        }
    }
    
    private GameObject CreateHitEffectPrefab()
    {
        GameObject effectObj = new GameObject("RhythmHitEffect");
        SpriteRenderer spriteRenderer = effectObj.AddComponent<SpriteRenderer>();
        Sprite sprite = CreateCircleSprite();
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = new Color(1, 0.8f, 0, 0.8f); // Golden yellow
        
        RhythmHitEffect effectComponent = effectObj.AddComponent<RhythmHitEffect>();
        effectComponent.duration = 0.5f;
        effectComponent.growSpeed = 4.0f;
        effectComponent.maxScale = 2.0f;
        effectComponent.startColor = new Color(1, 0.8f, 0, 0.8f);
        effectComponent.endColor = new Color(1, 0.5f, 0, 0);
        
        return effectObj;
    }
    
    private GameObject CreateRhythmHitTextPrefab()
    {
        // Create text object
        GameObject textObj = new GameObject("RhythmHitText");
        
        // Add Text component
        Text text = textObj.AddComponent<Text>();
        Font arialFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
        text.font = arialFont;
        text.text = "RHYTHM HIT!";
        text.fontSize = 24;
        text.color = new Color(1, 0.8f, 0, 1); // Golden
        text.alignment = TextAnchor.MiddleCenter;
        
        // Set RectTransform properties
        RectTransform rect = text.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 50);
        
        // Add animation component
        RhythmHitText hitText = textObj.AddComponent<RhythmHitText>();
        
        return textObj;
    }
}

// Simple text animation script
public class RhythmHitText : MonoBehaviour
{
    private float lifetime = 1.0f;
    private Vector3 moveSpeed = new Vector3(0, 100, 0);
    private float fadeSpeed = 1.0f;
    
    void Start()
    {
        StartCoroutine(Animate());
    }
    
    IEnumerator Animate()
    {
        Text text = GetComponent<Text>();
        float elapsed = 0;
        Color originalColor = text.color;
        
        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(1 - (elapsed * fadeSpeed / lifetime));
            
            // Move upward
            transform.position += moveSpeed * Time.deltaTime;
            
            // Fade out
            text.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            
            yield return null;
        }
        
        Destroy(gameObject);
    }
}
