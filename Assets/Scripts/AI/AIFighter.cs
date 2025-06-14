using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AIFighter : NewFighter
{
    [Header("AI Settings")]
    [Range(0f, 1f)] public float aggressiveness = 0.7f;
    [Range(0f, 1f)] public float defensiveness = 0.3f;
    [Range(0f, 1f)] public float randomness = 0.2f;
    public float decisionUpdateFrequency = 0.25f;
    
    // AI states
    private enum AIState { Idle, Approach, Attack, Defend, Retreat }
    private AIState currentState = AIState.Idle;
    
    // Decision making
    private float lastStateChangeTime;
    private float lastDecisionTime;
    private float stateChangeCooldown = 0.5f;
    private float distanceToOpponent;
    private bool opponentIsAttacking;
    private NewFighter opponent;
    
    // This method overrides the Update method in NewFighter
    // We need to make sure that Update is declared as virtual in NewFighter
    protected void AIUpdate()
    {
        if (opponent == null && FightManager.instance != null)
        {
            // Find opponent
            NewFighter[] fighters = FightManager.instance.GetFighters();
            foreach (NewFighter fighter in fighters)
            {
                if (fighter != this)
                {
                    opponent = fighter;
                    Debug.Log("AIFighter: Found opponent: " + fighter.name);
                    break;
                }
            }
        }
        
        if (opponent != null)
        {
            // Calculate distance to opponent
            distanceToOpponent = Vector3.Distance(transform.position, opponent.transform.position);
            
            // Check if opponent is attacking
            opponentIsAttacking = opponent.currentState is Attacking;
            
            // Make decisions based on current state and conditions
            if (Time.time >= lastDecisionTime + decisionUpdateFrequency)
            {
                MakeDecisions();
                lastDecisionTime = Time.time;
            }
        }
    }
    
    private void Start()
    {
        // Initialize decision making
        lastStateChangeTime = Time.time;
        lastDecisionTime = Time.time;
        Debug.Log("AIFighter: Initialized");
    }
    
    // This will be called after the base NewFighter.Update() method
    private void LateUpdate()
    {
        AIUpdate();
    }
    
    private void MakeDecisions()
    {
        // Possibly change state based on conditions
        if (Time.time >= lastStateChangeTime + stateChangeCooldown)
        {
            DecideNextState();
            lastStateChangeTime = Time.time;
        }
        
        // Execute current state behavior
        ExecuteCurrentState();
        Debug.Log("AIFighter: Current state: " + currentState + ", Distance: " + distanceToOpponent.ToString("F1"));
    }
    
    private void DecideNextState()
    {
        float random = Random.value;
        
        // Change state based on conditions and randomness
        if (opponentIsAttacking && Random.value < defensiveness)
        {
            currentState = AIState.Defend;
        }
        else if (distanceToOpponent > 3.0f)
        {
            currentState = AIState.Approach;
        }
        else if (distanceToOpponent < 1.5f && Random.value < aggressiveness)
        {
            currentState = AIState.Attack;
        }
        else if (currentHealth < opponent.currentHealth && Random.value < defensiveness)
        {
            currentState = AIState.Retreat;
        }
        else if (Random.value < randomness)
        {
            // Random state change
            currentState = (AIState)Random.Range(0, 5);
        }
    }
    
    // Here we use direct control methods rather than trying to simulate input
    private void ExecuteCurrentState()
    {
        Debug.Log("AIFighter: Executing state: " + currentState);
        
        // Create empty input data
        InputData simulatedInput = new InputData();
        simulatedInput.direction = 5; // Default to neutral
        
        // Execute behavior based on current state
        switch (currentState)
        {
            case AIState.Idle:
                // Do nothing, stay in neutral position
                break;
                
            case AIState.Approach:
                // Move toward opponent
                simulatedInput.direction = IsOnLeftSide ? 6 : 4;
                break;
                
            case AIState.Attack:
                // Choose an attack based on distance
                if (distanceToOpponent < 1.0f)
                {
                    // Close attack - maybe a grab or punch
                    simulatedInput.aPressed = Random.value > 0.5f;
                    simulatedInput.bPressed = !simulatedInput.aPressed;
                }
                else if (distanceToOpponent < 2.0f)
                {
                    // Medium attack
                    simulatedInput.bPressed = true;
                }
                else
                {
                    // Far attack - maybe a projectile
                    simulatedInput.cPressed = true;
                }
                break;
                
            case AIState.Defend:
                // Block by moving away
                simulatedInput.direction = IsOnLeftSide ? 4 : 6;
                break;
                
            case AIState.Retreat:
                // Move away from opponent
                simulatedInput.direction = IsOnLeftSide ? 4 : 6;
                // Maybe jump back
                if (Random.value < 0.3f)
                {
                    simulatedInput.jumpPressed = true;
                    simulatedInput.direction = IsOnLeftSide ? 7 : 9;
                }
                break;
        }
        
        // Set the input directly to base NewFighter class using reflection
        System.Reflection.FieldInfo inputField = typeof(NewFighter).GetField("currentInput", 
                                 System.Reflection.BindingFlags.NonPublic | 
                                 System.Reflection.BindingFlags.Instance | 
                                 System.Reflection.BindingFlags.Public);
                                 
        if (inputField != null)
        {
            inputField.SetValue(this, simulatedInput);
        }
    }
}
