using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NewFighter))]
public class FighterAI : MonoBehaviour
{
    [Header("References")]
    private NewFighter controlledFighter;
    private NewFighter opponentFighter;
    
    [Header("AI Settings")]
    [Range(0f, 1f)] public float aggressiveness = 0.7f;
    [Range(0f, 1f)] public float defensiveness = 0.3f;
    [Range(0f, 1f)] public float randomness = 0.2f;
    public float reactionTime = 0.1f;
    public float decisionUpdateFrequency = 0.25f;
    
    // AI states
    private enum AIState { Idle, Approach, Attack, Defend, Retreat }
    private AIState currentState = AIState.Idle;
    
    // Input simulation
    private InputData simulatedInput = new InputData();
    
    // Decision making
    private float lastStateChangeTime;
    private float lastDecisionTime;
    private float stateChangeCooldown = 0.5f;
    private float distanceToOpponent;
    private bool opponentIsAttacking;
    
    private void Start()
    {
        Debug.Log("FighterAI: Starting up");
        
        // Get the fighter component from this GameObject
        controlledFighter = GetComponent<NewFighter>();
        Debug.Log("FighterAI: Controlled fighter found: " + (controlledFighter != null));
        
        // Set fighter to AI controlled mode
        if (controlledFighter != null)
        {
            controlledFighter.isAIControlled = true;
            Debug.Log("FighterAI: Set fighter to AI controlled mode");
        }
            
        if (opponentFighter == null && FightManager.instance != null)
        {
            NewFighter[] fighters = FightManager.instance.GetFighters();
            Debug.Log("FighterAI: Found " + fighters.Length + " fighters in FightManager");
            
            foreach (NewFighter fighter in fighters)
            {
                if (fighter != controlledFighter)
                {
                    opponentFighter = fighter;
                    Debug.Log("FighterAI: Found opponent: " + fighter.name);
                    break;
                }
            }
        }
        
        // Initialize
        lastStateChangeTime = Time.time;
        lastDecisionTime = Time.time;
        
        // Create initial input data
        simulatedInput = new InputData();
        
        // Start decision making process
        StartCoroutine(AIDecisionMaking());
    }
    
    private void Update()
    {
        if (controlledFighter != null && opponentFighter != null)
        {
            // Calculate distance to opponent
            distanceToOpponent = Vector3.Distance(controlledFighter.transform.position, opponentFighter.transform.position);
            
            // Check if opponent is attacking
            opponentIsAttacking = opponentFighter.currentState is Attacking;
            
            // Make decisions based on current state and conditions
            if (Time.time >= lastDecisionTime + decisionUpdateFrequency)
            {
                MakeDecisions();
                lastDecisionTime = Time.time;
                
                // Apply the simulated input directly to the fighter
                controlledFighter.externalInput = simulatedInput;
                Debug.Log("FighterAI: Set input direction to " + simulatedInput.direction + 
                          ", A:" + simulatedInput.aPressed + ", B:" + simulatedInput.bPressed);
            }
        }
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
        else if (controlledFighter.currentHealth < opponentFighter.currentHealth && Random.value < defensiveness)
        {
            currentState = AIState.Retreat;
        }
        else if (Random.value < randomness)
        {
            // Random state change
            currentState = (AIState)Random.Range(0, 5);
        }
    }
    
    private void ExecuteCurrentState()
    {
        // Reset input
        simulatedInput = new InputData();
        simulatedInput.direction = 5; // Default to neutral
        
        // Debug state execution
        Debug.Log("FighterAI: Executing state: " + currentState);
        
        // Execute behavior based on current state
        switch (currentState)
        {
            case AIState.Idle:
                // Do nothing, stay in neutral position
                break;
                
            case AIState.Approach:
                // Move toward opponent
                simulatedInput.direction = controlledFighter.IsOnLeftSide ? 6 : 4;
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
                simulatedInput.direction = controlledFighter.IsOnLeftSide ? 4 : 6;
                break;
                
            case AIState.Retreat:
                // Move away from opponent
                simulatedInput.direction = controlledFighter.IsOnLeftSide ? 4 : 6;
                // Maybe jump back
                if (Random.value < 0.3f)
                {
                    simulatedInput.jumpPressed = true;
                    simulatedInput.direction = controlledFighter.IsOnLeftSide ? 7 : 9;
                }
                break;
        }
    }
    
    private IEnumerator AIDecisionMaking()
    {
        Debug.Log("FighterAI: Started decision making coroutine");
        
        while (true)
        {
            // Log the current state periodically
            Debug.Log("FighterAI: Current state: " + currentState + ", Distance: " + distanceToOpponent.ToString("F1") + 
                      ", Input direction: " + simulatedInput.direction);
            
            // Wait before next decision
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    private void OnEnable()
    {
        if (controlledFighter != null)
        {
            controlledFighter.isAIControlled = true;
        }
    }
    
    private void OnDisable()
    {
        if (controlledFighter != null)
        {
            controlledFighter.isAIControlled = false;
        }
    }
}
