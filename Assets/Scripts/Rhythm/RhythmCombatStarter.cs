using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AudioVisualizer;

/// <summary>
/// Add this script to your FightManager or any GameObject in the scene to automatically
/// set up and start the rhythm combat system with visualization and debug info.
/// </summary>
public class RhythmCombatStarter : MonoBehaviour
{
    // This script will automatically set everything up - no manual configuration needed!
    
    void Start()
    {
        Debug.Log("RhythmCombatStarter: Setting up rhythm combat system");
        StartCoroutine(DelayedSetup());
    }
    
    IEnumerator DelayedSetup()
    {
        // Wait a frame to ensure all components are loaded
        yield return null;
        
        // First, make sure AudioSampler exists and is initialized
        AudioSampler sampler = FindObjectOfType<AudioSampler>();
        if (sampler == null)
        {
            GameObject samplerObj = new GameObject("AudioSampler");
            sampler = samplerObj.AddComponent<AudioSampler>();
            
            // Initialize basics
            sampler.audioSources = new List<AudioSource>();
            
            // Make it persistent
            DontDestroyOnLoad(samplerObj);
            
            Debug.Log("RhythmCombatStarter: Created AudioSampler");
            
            // Wait another frame
            yield return null;
        }
        
        // Create AudioSource with music
        AudioSource musicSource = SetupMusic();
        
        // Add the music source to AudioSampler
        if (sampler.audioSources == null)
            sampler.audioSources = new List<AudioSource>();
        if (!sampler.audioSources.Contains(musicSource))
            sampler.audioSources.Add(musicSource);
            
        // Wait for AudioSampler to initialize
        yield return new WaitForSeconds(0.2f);
        
        // Create RhythmManager
        RhythmManager rhythmManager = SetupRhythmManager(musicSource);
        
        // Wait a frame
        yield return null;
        
        // Create UI elements for visualization
        SetupVisualization(rhythmManager);
        
        // Wait a frame
        yield return null;
        
        // Add RhythmCombatExtension to all fighters
        SetupFighters();
        
        // Update FightManager to work with rhythm system
        ConnectFightManager();
        
        Debug.Log("RhythmCombatStarter: Setup complete");
    }
    
    AudioSource SetupMusic()
    {
        // Check if we already have an AudioSource
        AudioSource source = GetComponent<AudioSource>();
        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
        }
        
        // Try to load a music clip
        AudioClip musicClip = Resources.Load<AudioClip>("BGM/fight_music");
        if (musicClip == null)
        {
            // Create simple beat audio if no music file found
            musicClip = CreateSimpleBeatClip();
        }
        
        source.clip = musicClip;
        source.loop = true;
        source.volume = 0.7f;
        source.Play();
        
        Debug.Log("RhythmCombatStarter: Music setup complete");
        return source;
    }
    
    AudioClip CreateSimpleBeatClip()
    {
        Debug.Log("RhythmCombatStarter: Creating test beat audio");
        
        int sampleRate = 44100;
        float length = 4f; // 4 second loop
        int samples = Mathf.FloorToInt(length * sampleRate);
        
        AudioClip clip = AudioClip.Create("TestBeat", samples, 1, sampleRate, false);
        float[] data = new float[samples];
        
        // Create a simple drum-like beat pattern
        for (int i = 0; i < 8; i++)
        {
            int beatStart = Mathf.FloorToInt(i * 0.5f * sampleRate);
            if (beatStart < data.Length)
            {
                // Create a beat sound (short decay sine wave)
                for (int j = 0; j < 4000 && (beatStart + j) < data.Length; j++)
                {
                    // Drum-like sound
                    data[beatStart + j] = Mathf.Sin(j * 0.05f) * (1f - (j / 4000f));
                }
            }
        }
        
        clip.SetData(data, 0);
        return clip;
    }
    
    RhythmManager SetupRhythmManager(AudioSource musicSource)
    {
        // Check if RhythmManager already exists
        RhythmManager manager = FindObjectOfType<RhythmManager>();
        if (manager == null)
        {
            GameObject managerObj = new GameObject("RhythmManager");
            manager = managerObj.AddComponent<RhythmManager>();
            manager.musicSource = musicSource;
            
            // Setup beat detection
            DontDestroyOnLoad(managerObj);
        }
        
        Debug.Log("RhythmCombatStarter: RhythmManager setup complete");
        return manager;
    }
    
    void SetupVisualization(RhythmManager rhythmManager)
    {
        // Create a canvas if one doesn't exist
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("RhythmCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Create debugging text
        GameObject textObj = new GameObject("RhythmDebugText");
        textObj.transform.SetParent(canvas.transform, false);
        
        Text text = textObj.AddComponent<Text>();
        text.text = "Rhythm Combat Active";
        text.fontSize = 24;
        text.color = Color.yellow;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        
        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(10, -10);
        rect.sizeDelta = new Vector2(300, 30);
        
        // Create beat indicator
        GameObject indicatorObj = new GameObject("BeatIndicator");
        indicatorObj.transform.SetParent(canvas.transform, false);
        
        Image indicatorImage = indicatorObj.AddComponent<Image>();
        indicatorImage.color = new Color(1, 0.8f, 0, 0.8f); // Golden yellow
        
        // Create a simple circle sprite
        indicatorImage.sprite = CreateCircleSprite();
        
        RectTransform indicatorRect = indicatorImage.GetComponent<RectTransform>();
        indicatorRect.anchorMin = new Vector2(0, 0);
        indicatorRect.anchorMax = new Vector2(0, 0);
        indicatorRect.pivot = new Vector2(0.5f, 0.5f);
        indicatorRect.anchoredPosition = new Vector2(50, 50);
        indicatorRect.sizeDelta = new Vector2(40, 40);
        
        // Add beat indicator component
        RhythmBeatIndicator beatIndicator = indicatorObj.AddComponent<RhythmBeatIndicator>();
        
        // Connect to rhythm manager
        rhythmManager.beatIndicatorPrefab = indicatorObj;
        rhythmManager.beatIndicatorParent = canvas.transform;
        
        Debug.Log("RhythmCombatStarter: Visualization setup complete");
    }
    
    Sprite CreateCircleSprite()
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
    
    void SetupFighters()
    {
        // Find all fighters
        NewFighter[] fighters = FindObjectsOfType<NewFighter>();
        if (fighters.Length == 0)
        {
            Debug.LogWarning("RhythmCombatStarter: No fighters found in scene");
            return;
        }
        
        Debug.Log($"RhythmCombatStarter: Found {fighters.Length} fighters");
        
        // Hit effect prefab
        GameObject hitEffectPrefab = new GameObject("RhythmHitEffect");
        SpriteRenderer renderer = hitEffectPrefab.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateCircleSprite();
        renderer.color = new Color(1, 1, 0, 0.8f);
        
        RhythmHitEffect hitEffect = hitEffectPrefab.AddComponent<RhythmHitEffect>();
        hitEffect.duration = 0.5f;
        hitEffect.growSpeed = 3.0f;
        hitEffect.maxScale = 2.0f;
        hitEffect.startColor = new Color(1, 1, 0, 0.8f);
        hitEffect.endColor = new Color(1, 1, 0, 0);
        
        // Hide the prefab
        hitEffectPrefab.SetActive(false);
        DontDestroyOnLoad(hitEffectPrefab);
        
        // Add combat extension to all fighters
        foreach (NewFighter fighter in fighters)
        {
            RhythmCombatExtension ext = fighter.GetComponent<RhythmCombatExtension>();
            if (ext == null)
            {
                ext = fighter.gameObject.AddComponent<RhythmCombatExtension>();
                ext.fighter = fighter;
                ext.onBeatHitEffectPrefab = hitEffectPrefab;
                ext.onBeatHitColor = Color.yellow;
                ext.maxComboMultiplier = 2.0f;
                ext.comboMultiplierIncrement = 0.2f;
                ext.onBeatDamageMultiplier = 1.5f;
                
                Debug.Log($"RhythmCombatStarter: Added RhythmCombatExtension to {fighter.name}");
            }
        }
    }
    
    void ConnectFightManager()
    {
        // Connect to fight manager
        FightManager fightManager = FindObjectOfType<FightManager>();
        if (fightManager != null)
        {
            // Create text prefab
            GameObject textPrefab = new GameObject("RhythmHitText");
            textPrefab.AddComponent<Text>();
            textPrefab.SetActive(false);
            DontDestroyOnLoad(textPrefab);
            
            // Connect to fight manager
            fightManager.rhythmHitTextPrefab = textPrefab;
            
            Debug.Log("RhythmCombatStarter: Connected to FightManager");
        }
    }
    
    void OnDestroy()
    {
        Debug.Log("RhythmCombatStarter: Destroyed");
    }
}
