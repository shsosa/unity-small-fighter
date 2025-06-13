using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A simplified rhythm system that works completely independently
/// No dependencies on AudioVisualizer or other external systems
/// </summary>
public class SimpleRhythmSystem : MonoBehaviour
{
    public static SimpleRhythmSystem instance;
    
    [Header("Audio")]
    public AudioSource musicSource;
    public float bpm = 120f;
    
    [Header("Rhythm Settings")]
    public float beatWindowSeconds = 0.15f;
    public float onBeatDamageMultiplier = 1.5f;
    public float maxComboMultiplier = 2.0f;
    public float comboMultiplierIncrement = 0.1f;
    
    [Header("Visualization")]
    public GameObject beatIndicator;
    public Text debugText;
    public Text hitNowText;
    public Image hitTimingBar;
    
    // Appearance settings
    public Color readyColor = Color.yellow;
    public Color perfectColor = Color.green;
    public Color missedColor = Color.red;
    
    // Events
    public event Action OnBeat;
    
    // Private variables
    private float secPerBeat;
    private float songPosition;
    private float songPositionInBeats;
    private float dspSongTime;
    private float lastBeatTime;
    private int lastBeatIndex = -1;
    private bool initialized = false;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        StartCoroutine(DelayedStart());
    }
    
    private IEnumerator DelayedStart()
    {
        yield return null;
        
        // Create music source if needed
        if (musicSource == null)
        {
            Debug.Log("SimpleRhythmSystem: Creating music source");
            musicSource = gameObject.AddComponent<AudioSource>();
            
            // Create a simple beat if no clip is assigned
            if (musicSource.clip == null)
            {
                musicSource.clip = CreateSimpleBeatClip();
                musicSource.loop = true;
                musicSource.volume = 0.7f;
            }
        }
        
        // Create UI elements if needed
        SetupUI();
        
        // Calculate timing values
        secPerBeat = 60.0f / bpm;
        
        // Start the music
        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
        
        // Record the time when the music starts
        dspSongTime = (float)AudioSettings.dspTime;
        lastBeatTime = (float)AudioSettings.dspTime;
        
        initialized = true;
        
        Debug.Log("SimpleRhythmSystem: Initialization complete");
    }
    
    private void Update()
    {
        if (!initialized || musicSource == null || !musicSource.isPlaying)
            return;
            
        // Calculate the song position
        songPosition = (float)(AudioSettings.dspTime - dspSongTime);
        
        // Calculate the song position in beats
        songPositionInBeats = songPosition / secPerBeat;
        
        // Calculate the current beat number
        int currentBeat = Mathf.FloorToInt(songPositionInBeats);
        
        // Check if we're on a new beat
        if (currentBeat > lastBeatIndex)
        {
            BeatDetected();
            lastBeatIndex = currentBeat;
        }
        
        // Calculate how far into the beat we are (0 to 1)
        float beatProgress = (songPositionInBeats - currentBeat);
        
        // Animate beat indicator if it exists
        if (beatIndicator != null)
        {
            // Scale the beat indicator based on the beat position
            float scale = 1.0f + 0.2f * Mathf.Sin(beatProgress * Mathf.PI * 2);
            beatIndicator.transform.localScale = new Vector3(scale, scale, 1f);
        }
        
        // Update timing bar and hit now text
        UpdateTimingVisuals(beatProgress);
        
        // Update debug text
        if (debugText != null)
        {
            debugText.text = $"Rhythm Combat Active\nBPM: {bpm}\nBeat: {currentBeat}";
        }
    }
    
    private void UpdateTimingVisuals(float beatProgress)
    {
        if (hitTimingBar != null)
        {
            // Update timing bar position/width
            RectTransform barRect = hitTimingBar.GetComponent<RectTransform>();
            
            // Perfect hit window is from beatWindowSeconds to 0 seconds before the beat
            // Show this as a position on the bar
            float barWidth = 400f; // Width of container
            float position = beatProgress * barWidth;
            barRect.sizeDelta = new Vector2(position, 0);
            
            // Update color based on timing
            bool inHitWindow = IsInHitWindow(beatProgress);
            hitTimingBar.color = inHitWindow ? perfectColor : readyColor;
            
            // Update "HIT NOW" text
            if (hitNowText != null)
            {
                if (inHitWindow)
                {
                    hitNowText.text = "HIT NOW!";
                    hitNowText.color = perfectColor;
                }
                else if (beatProgress > 0.85f) // About to enter hit window
                {
                    hitNowText.text = "GET READY...";
                    hitNowText.color = readyColor;
                }
                else
                {
                    hitNowText.text = "";
                }
            }
        }
    }
    
    private void BeatDetected()
    {
        // Record beat time
        lastBeatTime = (float)AudioSettings.dspTime;
        
        // Invoke beat event
        OnBeat?.Invoke();
        
        Debug.Log($"SimpleRhythmSystem: Beat detected at {lastBeatTime:F2}");
    }
    
    public bool IsOnBeat()
    {
        if (!initialized)
            return false;
            
        float timeSinceLastBeat = (float)AudioSettings.dspTime - lastBeatTime;
        return timeSinceLastBeat < beatWindowSeconds;
    }
    
    public bool IsInHitWindow(float beatProgress)
    {
        // The "perfect" window is right before the beat (0.85-1.0) or right after (0-0.15)
        return (beatProgress > (1.0f - beatWindowSeconds / secPerBeat)) || 
               (beatProgress < (beatWindowSeconds / secPerBeat));
    }
    
    private void SetupUI()
    {
        // Create a Canvas if none exists
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("RhythmCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Create debug text
        if (debugText == null)
        {
            GameObject textObj = new GameObject("RhythmDebugText");
            textObj.transform.SetParent(canvas.transform, false);
            
            debugText = textObj.AddComponent<Text>();
            debugText.text = "Rhythm System Active";
            debugText.fontSize = 24;
            debugText.color = Color.yellow;
            debugText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            
            RectTransform rect = debugText.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(10, -10);
            rect.sizeDelta = new Vector2(300, 80);
        }
        
        // Create beat indicator
        if (beatIndicator == null)
        {
            GameObject indicatorObj = new GameObject("BeatIndicator");
            indicatorObj.transform.SetParent(canvas.transform, false);
            
            Image indicatorImage = indicatorObj.AddComponent<Image>();
            indicatorImage.color = new Color(1, 0.8f, 0, 0.8f); // Golden yellow
            
            // Create a simple circle sprite
            indicatorImage.sprite = CreateCircleSprite();
            
            RectTransform indicatorRect = indicatorImage.GetComponent<RectTransform>();
            indicatorRect.anchorMin = new Vector2(0.5f, 0);
            indicatorRect.anchorMax = new Vector2(0.5f, 0);
            indicatorRect.pivot = new Vector2(0.5f, 0.5f);
            indicatorRect.anchoredPosition = new Vector2(0, 50);
            indicatorRect.sizeDelta = new Vector2(40, 40);
            
            beatIndicator = indicatorObj;
        }
        
        // Create timing bar
        if (hitTimingBar == null)
        {
            // Create container
            GameObject barContainerObj = new GameObject("TimingBarContainer");
            barContainerObj.transform.SetParent(canvas.transform, false);
            
            Image containerImage = barContainerObj.AddComponent<Image>();
            containerImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            RectTransform containerRect = containerImage.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0);
            containerRect.anchorMax = new Vector2(0.5f, 0);
            containerRect.pivot = new Vector2(0.5f, 0);
            containerRect.anchoredPosition = new Vector2(0, 120);
            containerRect.sizeDelta = new Vector2(400, 30);
            
            // Create timing bar
            GameObject barObj = new GameObject("TimingBar");
            barObj.transform.SetParent(barContainerObj.transform, false);
            
            hitTimingBar = barObj.AddComponent<Image>();
            hitTimingBar.color = readyColor;
            
            RectTransform barRect = hitTimingBar.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0, 0);
            barRect.anchorMax = new Vector2(0, 1);
            barRect.pivot = new Vector2(0, 0.5f);
            barRect.anchoredPosition = Vector2.zero;
            barRect.sizeDelta = new Vector2(50, 0);
        }
        
        // Create "HIT NOW" text
        if (hitNowText == null)
        {
            GameObject textObj = new GameObject("HitNowText");
            textObj.transform.SetParent(canvas.transform, false);
            
            hitNowText = textObj.AddComponent<Text>();
            hitNowText.text = "";
            hitNowText.fontSize = 36;
            hitNowText.color = perfectColor;
            hitNowText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hitNowText.alignment = TextAnchor.MiddleCenter;
            hitNowText.fontStyle = FontStyle.Bold;
            
            RectTransform rect = hitNowText.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, 160);
            rect.sizeDelta = new Vector2(300, 50);
        }
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
    
    private AudioClip CreateSimpleBeatClip()
    {
        Debug.Log("SimpleRhythmSystem: Creating test beat audio");
        
        int sampleRate = 44100;
        float length = 4f; // 4 second loop
        int samples = Mathf.FloorToInt(length * sampleRate);
        
        AudioClip clip = AudioClip.Create("TestBeat", samples, 1, sampleRate, false);
        float[] data = new float[samples];
        
        // Create a simple drum-like beat pattern (4/4 time)
        for (int i = 0; i < 4; i++)
        {
            int beatStart = Mathf.FloorToInt(i * 1.0f * sampleRate);
            if (beatStart < data.Length)
            {
                // Create a beat sound (short decay sine wave)
                for (int j = 0; j < 4000 && (beatStart + j) < data.Length; j++)
                {
                    data[beatStart + j] = Mathf.Sin(j * 0.05f) * (1f - (j / 4000f));
                }
            }
        }
        
        clip.SetData(data, 0);
        return clip;
    }
}
