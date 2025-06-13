using UnityEngine;
using UnityEngine.UI;
using AudioVisualizer;

[CreateAssetMenu(fileName = "RhythmCombatSetup", menuName = "Small Fighter/Rhythm Combat Setup")]
public class RhythmCombatSetup : MonoBehaviour
{
    [Header("Core Components")]
    public AudioClip backgroundMusic;
    public float musicVolume = 0.5f;
    public GameObject rhythmManagerPrefab;
    
    [Header("Beat Indicator")]
    public GameObject beatIndicatorPrefab;
    public Transform beatIndicatorParent;
    
    [Header("Combat Settings")]
    public float beatThreshold = 0.4f;
    public float rhythmWindowSeconds = 0.15f;
    public float onBeatMultiplier = 1.5f;
    public float maxComboMultiplier = 2.0f;
    
    [Header("Visual Effects")]
    public GameObject rhythmHitEffectPrefab;
    public GameObject rhythmHitTextPrefab;
    
    private void Start()
    {
        SetupRhythmSystem();
    }
    
    public void SetupRhythmSystem()
    {
        // Check if RhythmManager already exists
        if (RhythmManager.instance == null)
        {
            // Create RhythmManager
            GameObject rhythmManagerObject = null;
            
            if (rhythmManagerPrefab != null)
            {
                rhythmManagerObject = Instantiate(rhythmManagerPrefab);
            }
            else
            {
                rhythmManagerObject = new GameObject("RhythmManager");
                rhythmManagerObject.AddComponent<RhythmManager>();
            }
            
            RhythmManager rhythmManager = rhythmManagerObject.GetComponent<RhythmManager>();
            
            // Set up music source
            if (backgroundMusic != null)
            {
                AudioSource musicSource = rhythmManagerObject.GetComponent<AudioSource>();
                if (musicSource == null)
                {
                    musicSource = rhythmManagerObject.AddComponent<AudioSource>();
                }
                
                musicSource.clip = backgroundMusic;
                musicSource.volume = musicVolume;
                musicSource.loop = true;
                musicSource.Play();
                
                rhythmManager.musicSource = musicSource;
            }
            
            // Configure settings
            // Access the AudioEventListener component to set beat threshold
            AudioEventListener eventListener = rhythmManager.GetComponent<AudioEventListener>();
            if (eventListener != null)
            {
                eventListener.beatThreshold = beatThreshold;
            }
            
            rhythmManager.rhythmWindowSeconds = rhythmWindowSeconds;
            rhythmManager.onBeatDamageMultiplier = onBeatMultiplier;
            
            // Set up beat indicator
            if (beatIndicatorPrefab != null)
            {
                rhythmManager.beatIndicatorPrefab = beatIndicatorPrefab;
                rhythmManager.beatIndicatorParent = beatIndicatorParent;
            }
            
            // Make persistent
            DontDestroyOnLoad(rhythmManagerObject);
        }
        
        // Add RhythmCombatExtension to all fighters
        NewFighter[] fighters = FindObjectsOfType<NewFighter>();
        foreach (NewFighter fighter in fighters)
        {
            RhythmCombatExtension rhythmExt = fighter.GetComponent<RhythmCombatExtension>();
            if (rhythmExt == null)
            {
                rhythmExt = fighter.gameObject.AddComponent<RhythmCombatExtension>();
                rhythmExt.fighter = fighter;
                rhythmExt.maxComboMultiplier = maxComboMultiplier;
                rhythmExt.onBeatHitEffectPrefab = rhythmHitEffectPrefab;
            }
        }
        
        // Set up the text prefab on FightManager
        if (FightManager.instance != null && rhythmHitTextPrefab != null)
        {
            FightManager.instance.rhythmHitTextPrefab = rhythmHitTextPrefab;
        }
    }
}
