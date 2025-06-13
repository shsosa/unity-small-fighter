using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using AudioVisualizer;

public class RhythmManager : MonoBehaviour
{
    public static RhythmManager instance;
    
    [Header("Audio Settings")]
    public AudioSource musicSource;
    
    [Header("Rhythm Settings")]
    public float rhythmWindowSeconds = 0.15f;
    public float onBeatDamageMultiplier = 1.5f;
    
    [Header("Beat Indicator")]
    public GameObject beatIndicatorPrefab;
    public Transform beatIndicatorParent;
    
    private float lastBeatTime;
    private bool beatDetected = false;
    private AudioEventListener audioEventListener;
    
    // Event that fires when a beat is detected
    public event System.Action OnBeat;
    
    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
            
        DontDestroyOnLoad(gameObject);
    }
    
    // Delayed setup to ensure AudioSampler is initialized
    private IEnumerator DelayedSetup()
    {
        // Wait for AudioSampler to initialize
        yield return new WaitForSeconds(0.1f);
        
        // Setup audio event listener
        if (audioEventListener == null)
        {
            audioEventListener = GetComponent<AudioEventListener>();
            if (audioEventListener == null)
            {
                audioEventListener = gameObject.AddComponent<AudioEventListener>();
                audioEventListener.frequencyRange = FrequencyRange.LowMidrange;
                audioEventListener.beatThreshold = 1.3f;
                audioEventListener.automaticThreshold = true;
                audioEventListener.sampleBufferSize = 60;
                
                // Subscribe to beat event
                audioEventListener.OnBeat.AddListener(() => {
                    lastBeatTime = Time.time;
                    beatDetected = true;
                    OnBeat?.Invoke();
                });
            }
        }
        
        // Setup beat indicator
        SetupBeatIndicator();
        
        Debug.Log("RhythmManager: Delayed setup complete");
    }
    
    void Start()
    {
        // Ensure we have a music source
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // Make sure AudioSampler exists first
        AudioSampler sampler = FindObjectOfType<AudioSampler>();
        if (sampler == null)
        {
            // Create AudioSampler if it doesn't exist
            GameObject samplerObj = new GameObject("AudioSampler");
            sampler = samplerObj.AddComponent<AudioSampler>();
            
            // Initialize the AudioSampler manually before using it
            sampler.audioSources = new List<AudioSource>();
            sampler.audioSources.Add(musicSource);
            
            DontDestroyOnLoad(samplerObj);
            
            // Give it time to initialize
            StartCoroutine(DelayedSetup());
            return;
        }
        
        // Setup AudioSampler if it doesn't exist
        if (AudioSampler.instance == null)
        {
            GameObject audioSamplerObj = new GameObject("AudioSampler");
            AudioSampler audioSampler = audioSamplerObj.AddComponent<AudioSampler>();
            audioSampler.audioSources = new List<AudioSource> { musicSource };
            audioSampler.playOnAwake = true;
            DontDestroyOnLoad(audioSamplerObj);
        }
        else
        {
            // Add our music source to AudioSampler if it's not already there
            if (!AudioSampler.instance.audioSources.Contains(musicSource))
            {
                AudioSampler.instance.audioSources.Add(musicSource);
            }
        }
        
        // Setup AudioEventListener for beat detection
        audioEventListener = gameObject.AddComponent<AudioEventListener>();
        audioEventListener.audioIndex = AudioSampler.instance.audioSources.IndexOf(musicSource);
        audioEventListener.frequencyRange = FrequencyRange.LowMidrange; // Best for beat detection
        audioEventListener.beatThreshold = 1.3f; // Can be adjusted
        audioEventListener.automaticThreshold = true;
        audioEventListener.sampleBufferSize = 60;
        
        // Subscribe to beat events
        AudioEventListener.OnBeatRecognized += HandleBeat;
        
        // Start the visualizer polling
        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }
    
    void OnDestroy()
    {
        // Clean up event subscriptions
        AudioEventListener.OnBeatRecognized -= HandleBeat;
    }
    
    void HandleBeat(Beat beat)
    {
        lastBeatTime = Time.time;
        beatDetected = true;
        
        // Trigger beat event
        OnBeat?.Invoke();
            
        // Create beat indicator if prefab is assigned
        if (beatIndicatorPrefab != null && beatIndicatorParent != null)
        {
            GameObject indicator = Instantiate(beatIndicatorPrefab, beatIndicatorParent);
            indicator.transform.position = beatIndicatorParent.position;
        }
        
        StartCoroutine(ResetBeatDetectedFlag());
    }
    
    // Reset beat flag after the rhythm window time passes
    IEnumerator ResetBeatDetectedFlag()
    {
        yield return new WaitForSeconds(rhythmWindowSeconds);
        beatDetected = false;
    }
    
    // Check if current time is on a beat
    public bool IsOnBeat()
    {
        return beatDetected;
    }
    
    // Set up a simple beat indicator at runtime
    public void SetupBeatIndicator()
    {
        if (beatIndicatorPrefab == null)
        {
            // Create a canvas if one doesn't exist
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("RhythmCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            
            // Create the beat indicator prefab
            GameObject beatObj = new GameObject("BeatIndicator");
            beatObj.transform.SetParent(canvas.transform, false);
            UnityEngine.UI.Image image = beatObj.AddComponent<UnityEngine.UI.Image>();
            
            // Create a simple circle sprite
            Texture2D texture = new Texture2D(128, 128);
            Color[] colors = new Color[128 * 128];
            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    float dx = x - 64;
                    float dy = y - 64;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    if (dist < 60) // Circle radius
                        colors[y * 128 + x] = new Color(1, 1, 0, 1); // Yellow
                    else
                        colors[y * 128 + x] = new Color(0, 0, 0, 0); // Transparent
                }
            }
            texture.SetPixels(colors);
            texture.Apply();
            
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 128, 128), Vector2.one * 0.5f);
            image.sprite = sprite;
            image.color = new Color(1, 1, 0, 0.8f); // Yellow, slightly transparent
            
            // Position in bottom corner
            RectTransform rectTransform = beatObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 0);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(50, 50);
            rectTransform.sizeDelta = new Vector2(50, 50);
            
            // Add beat indicator component
            RhythmBeatIndicator indicator = beatObj.AddComponent<RhythmBeatIndicator>();
            
            beatIndicatorPrefab = beatObj;
            beatIndicatorParent = canvas.transform;
        }
    }
}
