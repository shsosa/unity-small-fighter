using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Setup for the SimpleRhythm system with inspector configuration.
/// Add this to any GameObject in your scene to initialize the rhythm system.
/// </summary>
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
        
        // Initialize UI elements
        // The SimpleRhythmSystem will create its own UI elements by default
        
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
