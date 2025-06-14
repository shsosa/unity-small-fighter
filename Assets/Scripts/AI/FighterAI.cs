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
    [Range(0f, 1f)] public float rhythmAwareness = 0.8f;  // How much the AI favors the beat
    public float decisionUpdateFrequency = 0.25f;
    public float reactionTime = 0.2f;
    
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
    private bool waitingForBeat = false;
    private float beatWaitStartTime = 0f;
    
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
            
            // If rhythm system exists, check for beat timing
            bool isOnBeat = SimpleRhythmSystem.instance?.IsOnBeat() ?? false;
            
            // If we're waiting for the beat and it's now on beat, attack immediately
            if (waitingForBeat && isOnBeat)
            {
                ExecuteRhythmAttack();
                waitingForBeat = false;
                lastDecisionTime = Time.time;
            }
            // If we've been waiting for the beat too long, attack anyway
            else if (waitingForBeat && (Time.time - beatWaitStartTime > 0.75f))
            {
                ExecuteRhythmAttack();
                waitingForBeat = false;
                lastDecisionTime = Time.time;
                Debug.Log("FighterAI: Beat wait timeout - attacking anyway");
            }
            // Otherwise, make normal decisions
            else if (!waitingForBeat && Time.time >= lastDecisionTime + decisionUpdateFrequency)
            {
                MakeDecisions(isOnBeat);
                lastDecisionTime = Time.time;
                
                // Apply the simulated input directly to the fighter
                controlledFighter.externalInput = simulatedInput;
                Debug.Log("FighterAI: Set input direction to " + simulatedInput.direction + 
                          ", A:" + simulatedInput.aPressed + ", B:" + simulatedInput.bPressed +
                          ", OnBeat:" + isOnBeat);
            }
        }
    }
    
    private void ExecuteRhythmAttack()
    {
        // Execute an attack timed with the beat
        simulatedInput = new InputData();
        simulatedInput.direction = 5; // Neutral position for attack
        
        // Choose attack based on distance
        if (distanceToOpponent < 1.5f)
        {
            // Close-range attack
            simulatedInput.aPressed = true;
        }
        else if (distanceToOpponent < 3.0f)
        {
            // Medium-range attack
            simulatedInput.bPressed = true;
        }
        else
        {
            // Long-range attack
            simulatedInput.cPressed = true;
        }
        
        // Apply the rhythm attack input
        controlledFighter.externalInput = simulatedInput;
        Debug.Log("FighterAI: Executing RHYTHM ATTACK with " + 
                  (simulatedInput.aPressed ? "A" : simulatedInput.bPressed ? "B" : "C"));
    }
    
    private void MakeDecisions(bool isOnBeat = false)
    {
        // Possibly change state based on conditions
        if (Time.time >= lastStateChangeTime + stateChangeCooldown)
        {
            DecideNextState(isOnBeat);
            lastStateChangeTime = Time.time;
        }
        
        // Execute current state behavior
        ExecuteCurrentState(isOnBeat);
        
        // Debug current state
        Debug.Log("FighterAI: Current state: " + currentState + ", Distance: " + distanceToOpponent.ToString("F1") + ", OnBeat: " + isOnBeat);
    }
    
    private void DecideNextState(bool isOnBeat = false)
    {
        float random = Random.value;
        
        // For rhythm-aware AI, favor attacks when on beat
        if (isOnBeat && Random.value < rhythmAwareness && distanceToOpponent < 4.0f)
        {
            // If we're close enough and on beat, prefer attacking
            currentState = AIState.Attack;
            Debug.Log("FighterAI: ON BEAT - Choosing to attack!");
        }
        else
        {
            // Normal decision logic
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
                // If we're not on beat but want to attack, maybe wait for the beat
                if (!isOnBeat && SimpleRhythmSystem.instance != null && Random.value < rhythmAwareness)
                {
                    // Queue an attack on beat
                    waitingForBeat = true;
                    beatWaitStartTime = Time.time;
                    currentState = AIState.Idle;
                    Debug.Log("FighterAI: Waiting for beat to attack");
                }
                else
                {
                    currentState = AIState.Attack;
                }
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
    }
    
    private void ExecuteCurrentState(bool isOnBeat = false)
    {
        Debug.Log("FighterAI: Executing state: " + currentState + (isOnBeat ? " ON BEAT" : ""));
        
        // Create empty input data
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
                // Block by moving away (holding back direction)
                simulatedInput.direction = controlledFighter.IsOnLeftSide ? 4 : 1;
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
