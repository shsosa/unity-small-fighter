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
    [Range(0f, 1f)] public float agilityFactor = 0.5f;    // How often AI will jump during movement
    public float decisionUpdateFrequency = 0.25f;
    public float reactionTime = 0.2f;
    
    [Header("Stage Settings")]
    public float leftBoundary = -8.5f;  // Default stage left boundary
    public float rightBoundary = 8.5f;  // Default stage right boundary
    
    [Header("Wall Detection")]
    public float raycastDistance = 2.0f;  // How far to check for walls
    public bool drawRaycastGizmos = true; // Draw debug rays in editor
    private int wallLayerMask;
    
    // AI states
    private enum AIState { Idle, Approach, Attack, Defend, Retreat, AvoidWall }
    private AIState currentState = AIState.Idle;
    
    // Input simulation
    private InputData simulatedInput = new InputData();
    
    // Decision making
    private float lastStateChangeTime;
    private float lastDecisionTime;
    private float stateChangeCooldown = 0.5f;
    private float distanceToOpponent;
    private float distanceToLeftWall;
    private float distanceToRightWall;
    private bool isNearWall = false;
    private bool isLeftWallDetected = false;
    private bool isRightWallDetected = false;
    private bool opponentIsAttacking;
    private bool waitingForBeat = false;
    private float beatWaitStartTime = 0f;
    private float lastJumpTime = 0f;
    private float jumpCooldown = 0.8f;  // Don't jump too frequently
    
    // Wall escape variables
    private float stuckTimer = 0f;
    private Vector3 lastPosition;
    private float stuckThreshold = 1.0f; // Time in seconds to consider fighter as "stuck"
    private int emergencyJumpCounter = 0;
    
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
        
        // Set wall detection layer mask - look for any collider
        wallLayerMask = LayerMask.GetMask("Default");
            
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
            
            // Reset wall detection flags
            isLeftWallDetected = false;
            isRightWallDetected = false;
            
            // Previous wall detection state
            bool wasNearWall = isNearWall;
            
            // Check using raycasts from the center of the fighter
            // Note: This will update distanceToLeftWall and distanceToRightWall with accurate values
            bool wallDetectedByRaycast = CheckWallsWithRaycasts();
            
            // Check if too close to walls using the updated distance values
            bool nearWallByPosition = distanceToLeftWall < 2.0f || distanceToRightWall < 2.0f;
            if (nearWallByPosition) {
                Debug.Log($"!!! WALL DETECTED (Position): Left = {distanceToLeftWall:F2}, Right = {distanceToRightWall:F2}, Position X = {transform.position.x:F2}");
            }
            
            // Consider near wall if either detection method returns true
            isNearWall = nearWallByPosition || wallDetectedByRaycast;
            
            // Log when we first detect a wall
            if (!wasNearWall && isNearWall) {
                Debug.Log($"!!! WALL HIT DETECTED !!! Position: {transform.position}, State: {currentState}");
                Debug.Log($"!!! WALL INFO: Left wall = {distanceToLeftWall:F2}, Right wall = {distanceToRightWall:F2}");
                Debug.Log($"!!! WALL DETAILS: Left detected: {isLeftWallDetected}, Right detected: {isRightWallDetected}");
            }
            
            // Check if fighter is stuck
            CheckIfStuck();
            
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
        
        // Wall avoidance takes highest priority
        if (isNearWall || stuckTimer > stuckThreshold * 0.5f)
        {
            // If we were already in wall avoidance and we're still stuck, force escape
            if (currentState == AIState.AvoidWall && stuckTimer > stuckThreshold)
            {
                ForceWallEscape();
            }
            else
            {
                currentState = AIState.AvoidWall;
                Debug.Log("FighterAI: Near wall, trying to escape");
            }
            return;
        }
        
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
                
                // Occasionally jump for more agile movement
                if (Random.value < agilityFactor * 0.3f && Time.time > lastJumpTime + jumpCooldown)
                {
                    simulatedInput.jumpPressed = true;
                    lastJumpTime = Time.time;
                    
                    // Sometimes do diagonal jumps
                    if (Random.value < 0.5f)
                    {
                        // Jump diagonally toward opponent
                        simulatedInput.direction = controlledFighter.IsOnLeftSide ? 9 : 7; // diagonal jump
                        Debug.Log("FighterAI: Agile diagonal jump toward opponent");
                    }
                    else
                    {
                        Debug.Log("FighterAI: Agile vertical jump while approaching");
                    }
                }
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
                
                // Jump back with higher probability for better agility
                if (Random.value < 0.45f && Time.time > lastJumpTime + jumpCooldown)
                {
                    simulatedInput.jumpPressed = true;
                    simulatedInput.direction = controlledFighter.IsOnLeftSide ? 7 : 9;
                    lastJumpTime = Time.time;
                    Debug.Log("FighterAI: Evasive jumping retreat");
                }
                break;
                
            case AIState.AvoidWall:
                // Enhanced wall escape logic - more aggressive and reliable
                // Use the explicit detection flags for better accuracy
                
                // Always try jumping if we're in wall avoidance state
                bool shouldJump = Time.time > lastJumpTime + jumpCooldown * 0.5f; // Reduced cooldown for wall escapes
                
                // Determine escape direction based on which wall was actually detected
                int escapeDirection;
                string wallSide;
                
                // Log detailed wall detection info for debugging
                Debug.Log($"WALL ESCAPE DECISION DATA:\n" + 
                          $"  Left wall detected: {isLeftWallDetected}, Left distance: {distanceToLeftWall:F2}\n" +
                          $"  Right wall detected: {isRightWallDetected}, Right distance: {distanceToRightWall:F2}\n" +
                          $"  Position X: {transform.position.x:F2}, Boundaries: [{leftBoundary:F2}, {rightBoundary:F2}]");
                
                // IMPORTANT: This logic determines which way to escape based on which wall was detected
                if (isLeftWallDetected || (!isRightWallDetected && distanceToLeftWall < distanceToRightWall)) {
                    // Near left wall - move RIGHT
                    wallSide = "LEFT";
                    escapeDirection = 6; // Move right absolutely
                    Debug.Log($"!!! DECISION: Escaping from LEFT wall by moving RIGHT (abs dir:{escapeDirection})");
                } else {
                    // Near right wall - move LEFT
                    wallSide = "RIGHT"; 
                    escapeDirection = 4; // Move left absolutely
                    Debug.Log($"!!! DECISION: Escaping from RIGHT wall by moving LEFT (abs dir:{escapeDirection})");
                }
                
                // Set movement direction
                simulatedInput.direction = escapeDirection;
                
                // Add jumps for faster escape
                if (shouldJump) {
                    simulatedInput.jumpPressed = true;
                    
                    // Use diagonal jump in the escape direction
                    if (wallSide == "LEFT") {
                        // Jump diagonally right (to escape left wall)
                        simulatedInput.direction = 9;
                        Debug.Log($"!!! ACTION: Jumping DIAGONALLY RIGHT (dir 9) to escape LEFT wall");
                    } else {
                        // Jump diagonally left (to escape right wall)
                        simulatedInput.direction = 7;
                        Debug.Log($"!!! ACTION: Jumping DIAGONALLY LEFT (dir 7) to escape RIGHT wall");
                    }
                    
                    lastJumpTime = Time.time;
                }
                
                // Transition out of wall avoidance faster once we're not near walls
                if (!isNearWall) {
                    // Force transition to approach state
                    currentState = AIState.Approach;
                    Debug.Log("FighterAI: Successfully escaped wall, returning to approach");
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
    
    // Check for nearby walls using raycasts from the fighter's center
    private bool CheckWallsWithRaycasts()
    {
        if (controlledFighter == null)
            return false;
            
        // Get the fighter's position and dimensions
        Vector3 position = transform.position;
        BoxCollider2D boxCollider = controlledFighter.boxCollider;
        
        // Calculate the center point of the fighter (use transform position if collider unavailable)
        Vector3 centerPoint = (boxCollider != null) ? boxCollider.bounds.center : position;
        
        // Direction for raycasts
        Vector3 leftDirection = Vector3.left;
        Vector3 rightDirection = Vector3.right;
        
        // Use a wider detection area to catch corners better
        float verticalOffset = 0.5f; // Check at different heights
        
        // Cast multiple rays in both horizontal directions at different heights
        bool leftWallDetected = false;
        bool rightWallDetected = false;
        
        // Check at center with 2D raycasts
        RaycastHit2D leftHit = Physics2D.Raycast(centerPoint, leftDirection, raycastDistance);
        RaycastHit2D rightHit = Physics2D.Raycast(centerPoint, rightDirection, raycastDistance);
        leftWallDetected |= leftHit.collider != null;
        rightWallDetected |= rightHit.collider != null;
        
        // Check above center with 2D raycasts
        Vector3 upperPoint = centerPoint + new Vector3(0, verticalOffset, 0);
        RaycastHit2D leftHitUpper = Physics2D.Raycast(upperPoint, leftDirection, raycastDistance);
        RaycastHit2D rightHitUpper = Physics2D.Raycast(upperPoint, rightDirection, raycastDistance);
        leftWallDetected |= leftHitUpper.collider != null;
        rightWallDetected |= rightHitUpper.collider != null;
        
        // Record which wall side was detected by raycast (used later for direction decisions)
        if (leftWallDetected) isLeftWallDetected = true;
        if (rightWallDetected) isRightWallDetected = true;
        
        // Debug visualization in scene view
        Debug.DrawRay(centerPoint, leftDirection * raycastDistance, leftWallDetected ? Color.red : Color.green);
        Debug.DrawRay(centerPoint, rightDirection * raycastDistance, rightWallDetected ? Color.red : Color.green);
        Debug.DrawRay(upperPoint, leftDirection * raycastDistance, leftWallDetected ? Color.red : Color.yellow);
        Debug.DrawRay(upperPoint, rightDirection * raycastDistance, rightWallDetected ? Color.red : Color.yellow);
        
        // Calculate more accurate distances to boundaries
        float actualLeftDistance = position.x - leftBoundary;
        float actualRightDistance = rightBoundary - position.x;
        
        // Check against absolute boundaries as an extra safety
        bool nearLeftBoundary = actualLeftDistance < 1.0f;
        bool nearRightBoundary = actualRightDistance < 1.0f;
        
        // Update the more accurate distances
        distanceToLeftWall = actualLeftDistance;
        distanceToRightWall = actualRightDistance;
        
        // Log if wall detected
        if (leftWallDetected)
            Debug.Log($"!!! RAYCAST: Wall detected on LEFT side (distance: {actualLeftDistance:F2})");
        if (rightWallDetected)
            Debug.Log($"!!! RAYCAST: Wall detected on RIGHT side (distance: {actualRightDistance:F2})");
        if (nearLeftBoundary)
            Debug.Log($"!!! BOUNDARY: Near LEFT boundary (distance: {actualLeftDistance:F2})");
        if (nearRightBoundary)
            Debug.Log($"!!! BOUNDARY: Near RIGHT boundary (distance: {actualRightDistance:F2})");
            
        return leftWallDetected || rightWallDetected || nearLeftBoundary || nearRightBoundary;
    }
    
    private void OnDrawGizmos()
    {
        // Only draw when enabled
        if (!drawRaycastGizmos || !enabled || controlledFighter == null) 
            return;
            
        // Draw wall detection rays in the editor
        if (controlledFighter != null && controlledFighter.boxCollider != null)
        {
            Vector3 centerPoint = controlledFighter.boxCollider.bounds.center;
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(centerPoint, Vector3.left * raycastDistance);
            Gizmos.DrawRay(centerPoint, Vector3.right * raycastDistance);
        }
    }
    
    // Check if fighter is stuck by monitoring position changes
    private void CheckIfStuck()
    {
        // If we're near a wall, check if we're actually moving
        if (isNearWall || currentState == AIState.AvoidWall)
        {
            // If this is the first frame we're checking
            if (lastPosition == Vector3.zero)
            {
                lastPosition = transform.position;
                return;
            }
            
            // Check if we've moved significantly
            float movedDistance = Vector3.Distance(transform.position, lastPosition);
            
            // If we haven't moved much
            if (movedDistance < 0.05f)
            {
                stuckTimer += Time.deltaTime;
                
                // Debug log
                if (stuckTimer > stuckThreshold)
                {
                    Debug.Log("FighterAI: Fighter appears to be STUCK! Forcing escape maneuver.");
                    ForceWallEscape();
                }
            }
            else
            {
                // We moved, reset stuck timer
                stuckTimer = 0f;
            }
            
            // Update last position
            lastPosition = transform.position;
        }
        else
        {
            // Not near wall, reset stuck detection
            stuckTimer = 0f;
            lastPosition = Vector3.zero;
            emergencyJumpCounter = 0;
        }
    }
    
    // Force escape from wall when stuck
    private void ForceWallEscape()
    {
        // Reset stuck timer
        stuckTimer = 0f;
        
        // Force state change to wall avoidance
        currentState = AIState.AvoidWall;
        
        // Force jump
        simulatedInput = new InputData();
        
        // Increment emergency counter
        emergencyJumpCounter++;
        
        // Determine which wall we're near
        bool nearLeftWall = distanceToLeftWall < distanceToRightWall;
        
        if (emergencyJumpCounter > 3)
        {
            // VERY aggressive escape - jump directly away AND up
            simulatedInput.jumpPressed = true;
            simulatedInput.direction = nearLeftWall ? 9 : 7; // Diagonal away from wall
            Debug.Log("FighterAI: EMERGENCY ESCAPE - Aggressive diagonal jump!");
        }
        else if (emergencyJumpCounter > 1)
        {
            // More aggressive escape - jump directly away from wall
            simulatedInput.jumpPressed = true;
            simulatedInput.direction = nearLeftWall ? 6 : 4; // Away from wall
            Debug.Log("FighterAI: EMERGENCY ESCAPE - Jumping away from wall");
        }
        else
        {
            // Initial escape - move away from wall
            simulatedInput.direction = nearLeftWall ? 6 : 4; // Away from wall
            Debug.Log("FighterAI: EMERGENCY ESCAPE - Moving away from wall");
        }
        
        // Apply input directly
        controlledFighter.externalInput = simulatedInput;
    }
}
