using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AudioVisualizer;

// Used to mark attacks that happen on beat
public class RhythmAttackMarker : MonoBehaviour
{
    public float damageMultiplier = 1.0f;
    public float timeOfAttack;
}

public class RhythmCombatExtension : MonoBehaviour
{
    public NewFighter fighter;
    
    [Header("Rhythm Settings")]
    public float comboMultiplierIncrement = 0.2f;
    public float maxComboMultiplier = 2.0f;
    public float onBeatDamageMultiplier = 1.5f;
    
    [Header("Visual Effects")]
    public GameObject onBeatHitEffectPrefab;
    public Color onBeatHitColor = Color.yellow;
    
    private float currentComboMultiplier = 1.0f;
    private int currentComboCount = 0;
    private bool wasAttacking = false;
    private RhythmAttackMarker currentAttackMarker;
    private SpriteRenderer[] fighterSprites;
    private List<Color> originalColors = new List<Color>();
    private bool isFlashing = false;
    
    void Awake()
    {
        Debug.Log($"RhythmCombatExtension: Initialized on {gameObject.name}");
        
        // Cache sprite renderers
        fighterSprites = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer renderer in fighterSprites)
        {
            originalColors.Add(renderer.color);
        }
    }
    
    void Start()
    {
        // Get fighter reference if not set
        if (fighter == null)
            fighter = GetComponent<NewFighter>();
            
        if (fighter == null)
        {
            Debug.LogError("RhythmCombatExtension: No fighter component found");
            enabled = false;
            return;
        }
        
        // Subscribe to AudioVisualizer beat events if available
        AudioEventListener[] listeners = FindObjectsOfType<AudioEventListener>();
        if (listeners.Length > 0)
        {
            Debug.Log("RhythmCombatExtension: Found AudioEventListener, subscribing to beat events");
        }
        else
        {
            Debug.LogWarning("RhythmCombatExtension: No AudioEventListener found");
        }
        
        StartCoroutine(MonitorAttacks());
    }
    
    private IEnumerator MonitorAttacks()
    {
        Debug.Log("RhythmCombatExtension: Starting attack monitoring");
        
        while (true)
        {
            // Check if fighter just started attacking
            bool isAttacking = fighter.currentState is Attacking && 
                              fighter.currentAction != null && 
                              !fighter.actionHasHit;
            if (isAttacking && !wasAttacking)
            {
                Debug.Log("RhythmCombatExtension: Detected attack start");
                
                // Check if attack is on beat
                bool isOnBeat = false;
                
                if (RhythmManager.instance != null && RhythmManager.instance.IsOnBeat())
                {
                    isOnBeat = true;
                }
                
                // Also check AudioVisualizer directly as backup
                AudioEventListener[] listeners = FindObjectsOfType<AudioEventListener>();
                foreach (AudioEventListener listener in listeners)
                {
                    // Try to access internal values via reflection to debug
                    string beatInfo = "unknown";
                    try 
                    {
                        var fieldInfo = typeof(AudioEventListener).GetField("canDetect", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (fieldInfo != null)
                        {
                            bool canDetect = (bool)fieldInfo.GetValue(listener);
                            beatInfo = canDetect.ToString();
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"RhythmCombatExtension: Error accessing AudioEventListener fields: {e.Message}");
                    }
                    
                    Debug.Log($"RhythmCombatExtension: AudioEventListener status - {beatInfo}");
                }
                
                if (isOnBeat)
                {
                    OnRhythmAttack();
                }
                else
                {
                    ResetCombo();
                }
            }
            
            // Update state tracking
            wasAttacking = fighter.currentState is Attacking && 
                           fighter.currentAction != null && 
                           !fighter.actionHasHit;
            
            yield return null;
        }
    }
    
    // Called when attack happens on the beat
    private void OnRhythmAttack()
    {
        // Increment combo
        currentComboCount++;
        currentComboMultiplier = Mathf.Min(
            1.0f + (comboMultiplierIncrement * currentComboCount),
            maxComboMultiplier
        );
        
        Debug.Log($"Rhythm Attack! Combo: {currentComboCount}, Multiplier: {currentComboMultiplier}");
        
        // Mark this as a rhythm attack
        if (currentAttackMarker == null)
        {
            currentAttackMarker = fighter.gameObject.AddComponent<RhythmAttackMarker>();
        }
        
        currentAttackMarker.damageMultiplier = currentComboMultiplier;
        currentAttackMarker.timeOfAttack = Time.time;
        
        // Visual feedback
        StartCoroutine(FlashSprites());
        
        // Particle effect
        if (onBeatHitEffectPrefab != null)
        {
            Vector3 effectPosition = fighter.transform.position + new Vector3(0, 1.0f, 0);
            GameObject effect = Instantiate(onBeatHitEffectPrefab, effectPosition, Quaternion.identity);
            Destroy(effect, 1.0f);
        }
        
        // Show rhythm hit message
        Debug.Log($"RHYTHM HIT! Combo x{currentComboCount}, Multiplier x{currentComboMultiplier:F1}");
    }
    
    // Reset combo when missing the beat
    private void ResetCombo()
    {
        if (currentComboCount > 0)
        {
            Debug.Log("RhythmCombatExtension: Combo reset");
        }
        
        currentComboCount = 0;
        currentComboMultiplier = 1.0f;
    }
    
    // Visual feedback with sprite flashing
    IEnumerator FlashSprites()
    {
        if (isFlashing) yield break;
        isFlashing = true;
        
        // Store original colors if not already stored
        if (originalColors.Count == 0 || originalColors.Count != fighterSprites.Length)
        {
            originalColors.Clear();
            foreach (SpriteRenderer renderer in fighterSprites)
            {
                originalColors.Add(renderer.color);
            }
        }
        
        // Flash to rhythm color
        for (int i = 0; i < fighterSprites.Length; i++)
        {
            if (fighterSprites[i] != null)
                fighterSprites[i].color = onBeatHitColor;
        }
        
        // Wait briefly
        yield return new WaitForSeconds(0.1f);
        
        // Restore colors
        for (int i = 0; i < fighterSprites.Length; i++)
        {
            if (fighterSprites[i] != null && i < originalColors.Count)
                fighterSprites[i].color = originalColors[i];
        }
        
        isFlashing = false;
    }

    // Apply bonus damage based on rhythm hit
    public void ApplyDamageBonus(ref int damage)
    {
        if (currentAttackMarker != null && Time.time - currentAttackMarker.timeOfAttack < 0.5f)
        {
            // Apply combo multiplier to damage
            int originalDamage = damage;
            damage = Mathf.RoundToInt(damage * currentAttackMarker.damageMultiplier);
            
            Debug.Log($"Applied rhythm bonus: {originalDamage} → {damage} (x{currentAttackMarker.damageMultiplier})");
            
            // Clean up marker
            Destroy(currentAttackMarker);
            currentAttackMarker = null;
            
            // Update last damage in FightManager
            if (FightManager.instance != null)
            {
                FightManager.instance.SetLastDamage(damage);
            }
        }
    }
    
    void OnDestroy()
    {
        // Clean up
        if (currentAttackMarker != null)
        {
            Destroy(currentAttackMarker);
        }
    }
}
