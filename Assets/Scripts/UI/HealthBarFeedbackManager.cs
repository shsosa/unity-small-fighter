using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manager class to initialize health bar feedback components and connect them to the FightManager
/// </summary>
public class HealthBarFeedbackManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image[] healthBarImages;
    [SerializeField] private Transform[] healthBarContainers;
    [SerializeField] private Canvas mainCanvas;
    
    // The feedback components we'll create
    private HealthBarFeedback[] healthBarFeedbacks;
    
    // Track last health values to detect changes
    private int[] previousHealthValues = new int[2];
    
    // Reference to SimpleRhythmSystem to check for rhythm hits
    private SimpleRhythmSystem rhythmSystem;
    
    // References to fighters
    private NewFighter[] fighters;
    
    private void Start()
    {
        // Find components if not assigned
        if (mainCanvas == null)
        {
            mainCanvas = FindObjectOfType<Canvas>();
        }
        
        FightManager fightManager = FightManager.instance;
        if (fightManager == null)
        {
            Debug.LogError("HealthBarFeedbackManager: FightManager not found in scene");
            return;
        }
        
        // Try to find healthbars through reflection if not assigned in inspector
        if (healthBarImages == null || healthBarImages.Length == 0)
        {
            System.Reflection.FieldInfo healthBarsField = typeof(FightManager).GetField("fighterHealthBars", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                
            if (healthBarsField != null)
            {
                healthBarImages = healthBarsField.GetValue(fightManager) as Image[];
            }
        }
        
        // Get fighters
        System.Reflection.FieldInfo fightersField = typeof(FightManager).GetField("fighters", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            
        if (fightersField != null)
        {
            fighters = fightersField.GetValue(fightManager) as NewFighter[];
        }
        
        // Try to find rhythm system
        rhythmSystem = FindObjectOfType<SimpleRhythmSystem>();
        
        InitFeedbackComponents();
        HookIntoEvents();
    }
    
    private void InitFeedbackComponents()
    {
        if (healthBarImages == null || healthBarImages.Length == 0)
        {
            Debug.LogError("HealthBarFeedbackManager: No health bar images assigned");
            return;
        }
        
        // Create feedback components for each health bar
        healthBarFeedbacks = new HealthBarFeedback[healthBarImages.Length];
        
        for (int i = 0; i < healthBarImages.Length; i++)
        {
            if (healthBarImages[i] == null) continue;
            
            // Create component on health bar's game object
            healthBarFeedbacks[i] = healthBarImages[i].gameObject.AddComponent<HealthBarFeedback>();
            
            // Set references (using reflection to set private fields directly)
            System.Reflection.FieldInfo healthBarField = typeof(HealthBarFeedback).GetField("healthBarImage", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
            if (healthBarField != null)
            {
                healthBarField.SetValue(healthBarFeedbacks[i], healthBarImages[i]);
            }
            
            // Set container (parent or self if no explicit container)
            System.Reflection.FieldInfo containerField = typeof(HealthBarFeedback).GetField("healthBarContainer", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
            Transform container = healthBarContainers != null && i < healthBarContainers.Length && healthBarContainers[i] != null
                ? healthBarContainers[i]
                : healthBarImages[i].transform;
                
            if (containerField != null)
            {
                containerField.SetValue(healthBarFeedbacks[i], container);
            }
            
            // Set damage text parent to canvas
            System.Reflection.FieldInfo parentField = typeof(HealthBarFeedback).GetField("damageTextParent", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
            if (parentField != null && mainCanvas != null)
            {
                parentField.SetValue(healthBarFeedbacks[i], mainCanvas.transform);
            }
            
            // Initialize previous health values
            if (fighters != null && i < fighters.Length && fighters[i] != null)
            {
                previousHealthValues[i] = fighters[i].currentHealth;
            }
        }
    }
    
    private void HookIntoEvents()
    {
        if (fighters == null) return;
        
        // Subscribe to TookDamage events for each fighter
        for (int i = 0; i < fighters.Length; i++)
        {
            if (fighters[i] != null)
            {
                int index = i; // Capture for lambda
                fighters[i].TookDamage.AddListener((fighter) => OnFighterTookDamage(fighter, index));
            }
        }
    }
    
    private void OnFighterTookDamage(NewFighter fighter, int fighterIndex)
    {
        if (healthBarFeedbacks == null || fighterIndex >= healthBarFeedbacks.Length || healthBarFeedbacks[fighterIndex] == null)
            return;
            
        // Calculate damage by comparing with previous health
        int damage = previousHealthValues[fighterIndex] - fighter.currentHealth;
        
        // Only process for actual damage taken
        if (damage > 0)
        {
            // Check if this was a rhythm-enhanced hit
            bool wasRhythmHit = IsRhythmHit(fighter);
            
            // Show feedback
            healthBarFeedbacks[fighterIndex].OnHealthChanged(
                fighter.currentHealth, 
                fighter.maxHealth, 
                wasRhythmHit
            );
        }
        
        // Update previous health
        previousHealthValues[fighterIndex] = fighter.currentHealth;
    }
    
    private bool IsRhythmHit(NewFighter fighter)
    {
        // If we have a rhythm system, check if the current fighter has a rhythm component
        if (rhythmSystem != null)
        {
            SimpleRhythmFighter[] rhythmFighters = FindObjectsOfType<SimpleRhythmFighter>();
            foreach (var rhythmFighter in rhythmFighters)
            {
                // Check if this rhythm fighter is the one that got hit
                if (rhythmFighter.gameObject == fighter.gameObject)
                {
                    // Get the last attack marker from the fighter to see if it was a rhythm hit
                    Component[] attackMarkers = rhythmFighter.GetComponentsInChildren(typeof(Component));
                    foreach (Component comp in attackMarkers)
                    {
                        if (comp.GetType().Name == "SimpleRhythmAttackMarker")
                        {
                            // Use reflection to check if this was a perfect hit
                            System.Reflection.PropertyInfo isPerfectHitProperty = 
                                comp.GetType().GetProperty("IsPerfectHit");
                                
                            if (isPerfectHitProperty != null)
                            {
                                object value = isPerfectHitProperty.GetValue(comp);
                                if (value is bool isPerfect)
                                {
                                    return isPerfect;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        return false;
    }
}
