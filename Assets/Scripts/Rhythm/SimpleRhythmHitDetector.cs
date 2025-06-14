using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Detects rhythm hits from SimpleRhythmFighter and SimpleRhythmAttackMarker and broadcasts them
/// to interested components like the HealthBarFeedback system
/// </summary>
[DefaultExecutionOrder(100)] // Execute after other rhythm system components
public class SimpleRhythmHitDetector : MonoBehaviour
{
    // Event that fires when a fighter lands a hit with information about whether it was a rhythm hit
    [System.Serializable]
    public class RhythmHitEvent : UnityEvent<NewFighter, NewFighter, bool, int> { }
    
    // Parameters: attacker, defender, wasRhythmHit, damage
    public RhythmHitEvent OnRhythmHit = new RhythmHitEvent();
    
    // Singleton instance for easy access
    public static SimpleRhythmHitDetector Instance { get; private set; }
    
    // References
    private SimpleRhythmSystem rhythmSystem;
    private List<SimpleRhythmFighter> rhythmFighters = new List<SimpleRhythmFighter>();
    private FightManager fightManager;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }
    }
    
    private void Start()
    {
        // Find necessary components
        rhythmSystem = FindObjectOfType<SimpleRhythmSystem>();
        fightManager = FightManager.instance;
        
        // Find and register all rhythm fighters
        FindAndRegisterFighters();
        
        // Hook into fight manager events
        HookIntoFightManager();
    }
    
    private void FindAndRegisterFighters()
    {
        SimpleRhythmFighter[] fighters = FindObjectsOfType<SimpleRhythmFighter>();
        foreach (var fighter in fighters)
        {
            RegisterFighter(fighter);
        }
    }
    
    public void RegisterFighter(SimpleRhythmFighter fighter)
    {
        if (!rhythmFighters.Contains(fighter))
        {
            rhythmFighters.Add(fighter);
        }
    }
    
    private void HookIntoFightManager()
    {
        if (fightManager == null) return;
        
        // Use reflection to access the private OnFighterHit method and add our handler
        // This is a bit of a hack, but it's better than modifying FightManager directly
        
        // Find the TookDamage events from fighters
        System.Reflection.FieldInfo fightersField = typeof(FightManager).GetField(
            "fighters", 
            System.Reflection.BindingFlags.Instance | 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Public);
            
        if (fightersField != null)
        {
            NewFighter[] fighters = fightersField.GetValue(fightManager) as NewFighter[];
            if (fighters != null)
            {
                foreach (var fighter in fighters)
                {
                    if (fighter != null)
                    {
                        fighter.TookDamage.AddListener(OnFighterTookDamage);
                    }
                }
            }
        }
    }
    
    private void OnFighterTookDamage(NewFighter defender)
    {
        // We need to determine:
        // 1. Which fighter hit this one (the attacker)
        // 2. Was it a rhythm hit?
        // 3. How much damage was done?
        
        // Get last damage from FightManager if possible
        int damage = 0;
        NewFighter attacker = null;
        
        // Get the last damage amount via reflection
        System.Reflection.FieldInfo lastDamageField = typeof(FightManager).GetField(
            "lastDamage",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic);
            
        if (lastDamageField != null && fightManager != null)
        {
            object value = lastDamageField.GetValue(fightManager);
            if (value is int lastDamage)
            {
                damage = lastDamage;
            }
        }
        
        // Try to find the attacker
        // In most cases it's the other fighter in a 1v1 game
        if (fightManager != null)
        {
            System.Reflection.FieldInfo fightersField = typeof(FightManager).GetField(
                "fighters", 
                System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Public);
                
            if (fightersField != null)
            {
                NewFighter[] fighters = fightersField.GetValue(fightManager) as NewFighter[];
                if (fighters != null && fighters.Length >= 2)
                {
                    // Attacker is most likely the one that's not the defender
                    attacker = (fighters[0] == defender) ? fighters[1] : fighters[0];
                }
            }
        }
        
        // Check if this was a rhythm hit by finding attack markers
        bool isRhythmHit = false;
        
        if (attacker != null)
        {
            SimpleRhythmFighter rhythmFighter = attacker.GetComponent<SimpleRhythmFighter>();
            if (rhythmFighter != null)
            {
                // Look for the latest attack marker
                SimpleRhythmAttackMarker[] markers = attacker.GetComponentsInChildren<SimpleRhythmAttackMarker>();
                if (markers != null && markers.Length > 0)
                {
                    // Use the most recently created marker (assuming it's the current attack)
                    foreach (var marker in markers)
                    {
                        if (marker.wasOnBeat)
                        {
                            isRhythmHit = true;
                            break;
                        }
                    }
                }
            }
        }
        
        // Broadcast the hit event
        OnRhythmHit.Invoke(attacker, defender, isRhythmHit, damage);
    }
}
