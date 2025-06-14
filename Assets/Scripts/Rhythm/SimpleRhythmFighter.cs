using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A simple rhythm-based fighter extension that works with SimpleRhythmSystem
/// </summary>
public class SimpleRhythmFighter : MonoBehaviour
{
    // Events for rhythm system feedback
    public event System.Action OnPerfectHit;
    public event System.Action OnMissedBeat;
    
    [Header("Fighter")]
    public NewFighter fighter;

    [Header("Rhythm Combat")]
    public float onBeatDamageMultiplier = 1.5f;
    public int maxComboCount = 10;
    public float maxComboMultiplier = 2.0f;
    public float comboMultiplierIncrement = 0.1f;

    [Header("Visual Feedback")]
    public GameObject hitEffectPrefab;
    public Color onBeatColor = Color.yellow;
    public float flashDuration = 0.1f;
    public TMPro.TextMeshProUGUI comboText; // UI text for combo display

    // Private variables
    private int _comboCount = 0;
    
    // Public accessor for combo count
    public int comboCount { get { return _comboCount; } }
    private float currentComboMultiplier = 1.0f;
    private bool wasAttacking = false;
    private SpriteRenderer[] fighterSprites;
    private List<Color> originalColors = new List<Color>();
    private bool isFlashing = false;
    private SimpleRhythmSystem rhythmSystem;
    private SimpleRhythmAttackMarker currentAttackMarker;

    private void Awake()
    {
        // Get the fighter component if not assigned
        if (fighter == null)
            fighter = GetComponent<NewFighter>();
            
        // Cache all sprite renderers
        fighterSprites = fighter.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sprite in fighterSprites)
        {
            originalColors.Add(sprite.color);
        }
    }

    private void Start()
    {
        // Find or create the rhythm system
        rhythmSystem = FindObjectOfType<SimpleRhythmSystem>();
        if (rhythmSystem == null)
        {
            GameObject obj = new GameObject("SimpleRhythmSystem");
            rhythmSystem = obj.AddComponent<SimpleRhythmSystem>();
        }
        
        // Start monitoring attacks
        StartCoroutine(MonitorAttacks());
        
        Debug.Log($"SimpleRhythmFighter: Initialized for {fighter.name}");
    }

    private IEnumerator MonitorAttacks()
    {
        Debug.Log("SimpleRhythmFighter: Started monitoring attacks");
        bool wasActionHasHit = false;
        
        while (true)
        {
            // Check if fighter just started attacking
            bool isAttacking = fighter.currentState is Attacking && 
                               fighter.currentAction != null;
            
            // Track when attack starts (but don't trigger effects yet)
            if (isAttacking && !wasAttacking)
            {
                Debug.Log("SimpleRhythmFighter: Detected attack start");
                
                // Check if the attack is on beat
                if (rhythmSystem != null && rhythmSystem.IsOnBeat())
                {
                    // Mark this attack for later when it hits
                    currentAttackMarker = fighter.gameObject.AddComponent<SimpleRhythmAttackMarker>();
                    currentAttackMarker.timeOfAttack = Time.time;
                    currentAttackMarker.damageMultiplier = currentComboMultiplier;
                    currentAttackMarker.wasOnBeat = true;
                }
                else
                {
                    // Off-beat attack
                    currentAttackMarker = fighter.gameObject.AddComponent<SimpleRhythmAttackMarker>();
                    currentAttackMarker.wasOnBeat = false;
                }
            }
            
            // Check if an attack just landed (actionHasHit changed from false to true)
            if (isAttacking && fighter.actionHasHit && !wasActionHasHit)
            {
                Debug.Log("SimpleRhythmFighter: Attack landed");
                
                // Check if the attack was marked as on-beat
                if (currentAttackMarker != null && currentAttackMarker.wasOnBeat)
                {
                    // Rhythm hit with successful contact!
                    OnRhythmAttack();
                    
                    // Trigger the perfect hit event for visual feedback
                    OnPerfectHit?.Invoke();
                }
                else if (currentAttackMarker != null && !currentAttackMarker.wasOnBeat)
                {
                    // Attack landed but wasn't on beat
                    ResetCombo();
                    
                    // Trigger the missed beat event for visual feedback
                    OnMissedBeat?.Invoke();
                }
            }
            
            // Track action hit state
            wasActionHasHit = isAttacking && fighter.actionHasHit;
            
            // Update state tracking
            wasAttacking = isAttacking;
            
            yield return null;
        }
    }

    private void OnRhythmAttack()
    {
        // Increment combo
        _comboCount++;
        
        // Update multiplier based on combo count
        currentComboMultiplier = 1.0f + (Mathf.Min(_comboCount, maxComboCount) * comboMultiplierIncrement);
            
        Debug.Log($"SimpleRhythmFighter: Rhythm hit! Combo: {comboCount}, Multiplier: {currentComboMultiplier:F1}x");
        
        // Visual feedback
        StartCoroutine(FlashSprites());
        
        // Create hit effect
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                hitEffectPrefab, 
                fighter.transform.position + new Vector3(0, 1, 0), 
                Quaternion.identity);
                
            Destroy(effect, 1.0f);
        }
        
        // Show debug message
        Debug.Log($"RHYTHM HIT! Combo x{comboCount}, Multiplier x{currentComboMultiplier:F1}");
    }

    private void ResetCombo()
    {
        if (comboCount > 0)
        {
            Debug.Log($"SimpleRhythmFighter: Combo reset (was {comboCount})");
            _comboCount = 0;
            currentComboMultiplier = 1.0f;
        }
    }

    private IEnumerator FlashSprites()
    {
        if (isFlashing)
            yield break;
            
        isFlashing = true;
        
        // Flash sprites
        for (int i = 0; i < fighterSprites.Length; i++)
        {
            if (fighterSprites[i] != null)
                fighterSprites[i].color = onBeatColor;
        }
        
        // Wait for flash duration
        yield return new WaitForSeconds(flashDuration);
        
        // Restore colors
        for (int i = 0; i < fighterSprites.Length; i++)
        {
            if (fighterSprites[i] != null && i < originalColors.Count)
                fighterSprites[i].color = originalColors[i];
        }
        
        isFlashing = false;
    }

    // This method should be called from the damage system to apply rhythm damage bonus
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
