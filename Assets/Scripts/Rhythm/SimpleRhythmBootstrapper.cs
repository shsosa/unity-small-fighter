using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Add this single component to any GameObject in your scene to fully set up the rhythm combat system.
/// No other manual setup is required.
/// </summary>
public class SimpleRhythmBootstrapper : MonoBehaviour
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
    
    private SimpleRhythmSystem rhythmSystem;
    
    void Start()
    {
        Debug.Log("SimpleRhythmBootstrapper: Starting setup");
        StartCoroutine(SetupRhythmSystem());
    }
    
    IEnumerator SetupRhythmSystem()
    {
        // Wait a frame to ensure all other components are initialized
        yield return null;
        
        // Create the rhythm system
        GameObject rhythmObj = new GameObject("SimpleRhythmSystem");
        rhythmSystem = rhythmObj.AddComponent<SimpleRhythmSystem>();
        
        // Configure rhythm system
        rhythmSystem.bpm = bpm;
        rhythmSystem.beatWindowSeconds = beatWindowSeconds;
        rhythmSystem.onBeatDamageMultiplier = onBeatDamageMultiplier;
        rhythmSystem.maxComboMultiplier = maxComboMultiplier;
        rhythmSystem.comboMultiplierIncrement = comboMultiplierIncrement;
        
        // Setup audio
        SetupAudio();
        
        // Wait a frame
        yield return null;
        
        // Add rhythm components to all fighters
        SetupFighters();
        
        Debug.Log("SimpleRhythmBootstrapper: Setup complete");
    }
    
    void SetupAudio()
    {
        // Create audio source if needed
        if (rhythmSystem.musicSource == null)
        {
            rhythmSystem.musicSource = rhythmSystem.gameObject.AddComponent<AudioSource>();
        }
        
        // Set up music clip
        if (musicClip != null)
        {
            rhythmSystem.musicSource.clip = musicClip;
        }
        
        rhythmSystem.musicSource.volume = volume;
        rhythmSystem.musicSource.loop = true;
        
        Debug.Log("SimpleRhythmBootstrapper: Audio setup complete");
    }
    
    void SetupFighters()
    {
        // Find all fighters in the scene
        NewFighter[] fighters = FindObjectsOfType<NewFighter>();
        
        if (fighters.Length == 0)
        {
            Debug.LogWarning("SimpleRhythmBootstrapper: No fighters found in the scene");
            return;
        }
        
        // Create hit effect prefab
        GameObject hitEffectPrefab = CreateHitEffectPrefab();
        
        // Add rhythm component to all fighters
        foreach (NewFighter fighter in fighters)
        {
            // Check if the fighter already has a rhythm component
            SimpleRhythmFighter rhythmFighter = fighter.GetComponent<SimpleRhythmFighter>();
            if (rhythmFighter == null)
            {
                rhythmFighter = fighter.gameObject.AddComponent<SimpleRhythmFighter>();
                rhythmFighter.fighter = fighter;
                rhythmFighter.onBeatDamageMultiplier = onBeatDamageMultiplier;
                rhythmFighter.maxComboMultiplier = maxComboMultiplier;
                rhythmFighter.comboMultiplierIncrement = comboMultiplierIncrement;
                rhythmFighter.hitEffectPrefab = hitEffectPrefab;
                
                Debug.Log($"SimpleRhythmBootstrapper: Added SimpleRhythmFighter to {fighter.name}");
            }
        }
    }
    
    GameObject CreateHitEffectPrefab()
    {
        // Create a simple hit effect prefab
        GameObject prefab = new GameObject("RhythmHitEffect");
        
        // Add sprite renderer
        SpriteRenderer renderer = prefab.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateCircleSprite();
        renderer.color = new Color(1, 1, 0, 0.8f);
        
        // Add a simple animation script
        SimpleHitEffect hitEffect = prefab.AddComponent<SimpleHitEffect>();
        
        // Hide and persist the prefab
        prefab.SetActive(false);
        DontDestroyOnLoad(prefab);
        
        return prefab;
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
}

/// <summary>
/// Simple hit effect that grows and fades out
/// </summary>
public class SimpleHitEffect : MonoBehaviour
{
    public float duration = 0.5f;
    public float growSpeed = 2.0f;
    
    private float startTime;
    private Vector3 originalScale;
    
    void OnEnable()
    {
        startTime = Time.time;
        originalScale = transform.localScale;
    }
    
    void Update()
    {
        float elapsed = Time.time - startTime;
        float progress = elapsed / duration;
        
        if (progress >= 1.0f)
        {
            Destroy(gameObject);
            return;
        }
        
        // Grow
        transform.localScale = originalScale * (1.0f + progress * growSpeed);
        
        // Fade
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            Color color = renderer.color;
            color.a = Mathf.Lerp(1.0f, 0.0f, progress);
            renderer.color = color;
        }
    }
}
