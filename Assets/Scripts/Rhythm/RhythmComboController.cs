using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls rhythm-based combo sequences that change attacks based on successful rhythm hits.
/// When the player hits the same button on rhythm, this controller will cycle through different attacks.
/// </summary>
public class RhythmComboController : MonoBehaviour
{
    [Header("Combo Settings")]
    [Tooltip("Scriptable Object defining the combo sequence")]
    [SerializeField] private RhythmComboActionSO comboDefinition;
    
    [Header("Input Settings")]
    private PlayerInput playerInput;
    [SerializeField] private string rhythmComboActionName = "Attack1"; // Use the same action as normal attack
    
    [Header("Feedback")]
    [SerializeField] private float comboTextSize = 1.5f;
    
    // Fallback settings if no scriptable object is provided
    [Header("Manual Combo Sequence")]
    [Tooltip("List of actions to cycle through on rhythm hits (if no ComboDefinition is set)")]
    [SerializeField] private List<ActionData> fallbackComboSequence = new List<ActionData>();
    [SerializeField] private int missesToReset = 2;
    
    // Current state
    private int currentComboIndex = 0;
    private int successfulHits = 0;
    private int missedHits = 0;
    private NewFighter attachedFighter;
    private SimpleRhythmSystem rhythmSystem;
    
    // List to track combo actions if using scriptable object
    private List<RhythmComboActionSO.ComboAction> comboActions = new List<RhythmComboActionSO.ComboAction>();
    
    // Track if we're already in a combo
    private bool inComboSequence = false;
    private bool comboActive = false;
    private bool wasComboProcessedThisFrame = false;
    private float lastHitTime = 0f;
    
    void Start()
    {
        // Get the fighter component
        attachedFighter = GetComponent<NewFighter>();
        
        // Initialize the PlayerInput reference
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogError("RhythmComboController requires a PlayerInput component");
            return;
        }

        if (attachedFighter == null)
        {
            Debug.LogError("RhythmComboController requires a NewFighter component");
            return;
        }
        
        // Initialize combo actions from the ScriptableObject if available
        if (comboDefinition != null)
        {
            comboActions = new List<RhythmComboActionSO.ComboAction>(comboDefinition.comboSequence);
            missesToReset = comboDefinition.missesToReset;
            Debug.Log($"Loaded {comboActions.Count} combo actions from {comboDefinition.name}");
        }
        else
        {
            Debug.LogWarning("No RhythmComboActionSO assigned! Using fallback combo sequence.");
        }
        
        // Find or create rhythm system
        if (rhythmSystem == null)
        {
            GameObject obj = new GameObject("SimpleRhythmSystem");
            rhythmSystem = obj.AddComponent<SimpleRhythmSystem>();
        }
    }
    
    void Update()
    {
        // We'll now handle input via the OnAttack method registered to the InputSystem events
    }
    
    void OnEnable()
    {
        if (playerInput != null)
        {
            // Subscribe to the attack action
            playerInput.actions[rhythmComboActionName].performed += OnAttackInput;
        }
    }
    
    void OnDisable()
    {
        if (playerInput != null)
        {
            // Unsubscribe from the attack action
            playerInput.actions[rhythmComboActionName].performed -= OnAttackInput;
        }
    }
    
    /// <summary>
    /// Called when the attack input is detected
    /// </summary>
    private void OnAttackInput(InputAction.CallbackContext context)
    {
        Debug.Log("Attack input detected!");
        
        // Check if the keypress is on beat
        bool isOnBeat = rhythmSystem != null && rhythmSystem.IsOnBeat();
        
        Debug.Log($"Is on beat: {isOnBeat}");
        
        if (isOnBeat)
        {
            // Successful rhythm hit
            OnRhythmHitDetected();
            
            // Execute the current combo action
            ExecuteComboAction();
        }
        else
        {
            // Missed the beat
            OnRhythmMissDetected();
        }
    }
    
    /// <summary>
    /// Called when a rhythm hit is detected
    /// </summary>
    public void OnRhythmHitDetected()
    {
        successfulHits++;
        missedHits = 0;
        
        // When using ScriptableObject, progress is handled by hits required per action
        if (comboActions.Count > 0)
        {
            int hitsNeeded = comboActions[currentComboIndex].hitsToProgress;
            if (successfulHits >= hitsNeeded)
            {
                // Progress to next combo action
                ProgressCombo();
                successfulHits = 0;
            }
        }
        else if (fallbackComboSequence.Count > 0)
        {
            // Using fallback combo sequence (legacy support)
            if (successfulHits >= 1) // Default 1 hit to progress
            {
                ProgressCombo();
                successfulHits = 0;
            }
        }
        
        // Visual feedback
        ShowComboStatus();
    }
    

    
    /// <summary>
    /// Called when a rhythm miss is detected
    /// </summary>
    public void OnRhythmMissDetected()
    {
        missedHits++;
        
        // Check if we need to reset the combo
        if (missedHits >= missesToReset)
        {
            ResetCombo();
        }
    }
    
    /// <summary>
    /// Execute the current action in the combo sequence
    /// </summary>
    private void ExecuteComboAction()
    {
        // Do nothing if fighter is missing
        if (attachedFighter == null) return;
        
        ActionData actionToExecute = null;
        string actionName = "";
        
        // Get the current action based on combo index
        if (comboActions.Count > 0 && currentComboIndex < comboActions.Count)
        {
            // Using ScriptableObject combo definition
            actionToExecute = comboActions[currentComboIndex].action;
            actionName = comboActions[currentComboIndex].actionName;
        }
        else if (fallbackComboSequence.Count > 0 && currentComboIndex < fallbackComboSequence.Count)
        {
            // Using fallback sequence
            actionToExecute = fallbackComboSequence[currentComboIndex];
            actionName = actionToExecute.actionName;
        }
        
        if (actionToExecute != null)
        {            
            // Temporarily make the fighter AI controlled to inject our action
            bool wasAIControlled = attachedFighter.isAIControlled;
            attachedFighter.isAIControlled = true;
            
            // Create input data that will trigger a specific action
            InputData actionInput = new InputData();
            actionInput.aPressed = true; // This will trigger the attack system
            
            // Set external input to trigger the action
            attachedFighter.externalInput = actionInput;
                
            // Set the current action directly instead of using OverrideAction
            attachedFighter.currentAction = actionToExecute;
                
            Debug.Log($"Executed combo action: {actionName}");
            
            // Reset AI controlled state and input after a delay
            StartCoroutine(ResetFighterState(wasAIControlled));
        }
    }
    
    /// <summary>
    /// Reset fighter state after executing an action
    /// </summary>
    private System.Collections.IEnumerator ResetFighterState(bool originalAIState)
    {
        // Wait a brief moment for the action to be processed
        yield return new WaitForSeconds(0.05f);
        
        // Reset fighter state
        if (attachedFighter != null)
        {
            attachedFighter.isAIControlled = originalAIState;
            attachedFighter.externalInput = new InputData();
        }
    }
    
    /// <summary>
    /// Progress to the next attack in the combo sequence
    /// </summary>
    private void ProgressCombo()
    {
        // Using ScriptableObject combo actions
        if (comboActions.Count > 0)
        {
            // Increment combo index
            currentComboIndex++;
            
            // Check if we've reached the end of the sequence
            if (currentComboIndex >= comboActions.Count)
            {
                // Reached the end of the sequence, reset and start over
                Debug.Log("Combo sequence completed! Resetting to first action.");
                ResetCombo();
                return;
            }
            
            inComboSequence = true;
            
            // Feedback for new combo level
            Debug.Log($"Combo progressed to: {comboActions[currentComboIndex].actionName}");
        }
        // Using fallback sequence
        else if (fallbackComboSequence.Count > 0)
        {
            // Increment combo index
            currentComboIndex++;
            
            // Check if we've reached the end of the sequence
            if (currentComboIndex >= fallbackComboSequence.Count)
            {
                // Reached the end of the sequence, reset and start over
                Debug.Log("Combo sequence completed! Resetting to first action.");
                ResetCombo();
                return;
            }
            
            inComboSequence = true;
            
            // Feedback for new combo level
            Debug.Log($"Combo progressed to: {fallbackComboSequence[currentComboIndex].actionName}");
        }
    }
    
    /// <summary>
    /// Reset the combo sequence
    /// </summary>
    private void ResetCombo()
    {
        // Reset all combo state variables
        currentComboIndex = 0;
        successfulHits = 0;
        missedHits = 0;
        inComboSequence = false;
        comboActive = false;
        wasComboProcessedThisFrame = false;
        
        // Force lastHitTime to a value that won't trigger a timeout immediately
        lastHitTime = Time.time;
        
        // Verify the current action that will be used next
        ActionData nextAction = null;
        if (comboActions.Count > 0)
        {
            nextAction = comboActions[0].action;
            Debug.Log($"[DEBUG] Combo FULLY RESET to first action: {comboActions[0].actionName}");
        }
        else if (fallbackComboSequence.Count > 0)
        {
            nextAction = fallbackComboSequence[0];
            Debug.Log($"[DEBUG] Combo FULLY RESET to first fallback action");
        }
        else
        {
            Debug.LogWarning("[DEBUG] Combo reset but no actions are defined!");
        }
    }
    
    /// <summary>
    /// Get the current action in the sequence based on combo progress
    /// </summary>
    public ActionData GetCurrentComboAction()
    {
        if (comboActions.Count > 0 && currentComboIndex < comboActions.Count)
        {
            return comboActions[currentComboIndex].action;
        }
        else if (fallbackComboSequence.Count > 0 && currentComboIndex < fallbackComboSequence.Count)
        {
            return fallbackComboSequence[currentComboIndex];
        }
        
        return null;
    }
    
    /// <summary>
    /// Show combo status feedback on screen
    /// </summary>
    private void ShowComboStatus()
    {
        // Get current action name from either source
        string actionName = "";
        if (comboActions.Count > 0 && currentComboIndex < comboActions.Count)
        {
            actionName = comboActions[currentComboIndex].actionName;
        }
        else if (fallbackComboSequence.Count > 0 && currentComboIndex < fallbackComboSequence.Count)
        {
            ActionData fallbackAction = fallbackComboSequence[currentComboIndex];
            actionName = fallbackAction != null ? fallbackAction.actionName : "Unknown";
        }
        else
        {
            return; // No valid combo to display
        }
        
        // Create a text message showing the current combo level and next attack
        string comboText = $"RHYTHM COMBO {currentComboIndex + 1}: {actionName}";
        
        // Find a canvas to use
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("RhythmComboCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        
        // Display the message using the UI system
        GameObject textObj = new GameObject("ComboStatusText");
        textObj.transform.SetParent(canvas.transform, false);
        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        
        // Position at the bottom of the screen
        rectTransform.anchorMin = new Vector2(0.5f, 0);
        rectTransform.anchorMax = new Vector2(0.5f, 0);
        rectTransform.pivot = new Vector2(0.5f, 0);
        rectTransform.anchoredPosition = new Vector2(0, 100);
        rectTransform.sizeDelta = new Vector2(400, 50);
        
        // Add TMPro text component for better visuals
        TMPro.TextMeshProUGUI text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        text.fontSize = 24;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.color = comboDefinition != null ? comboDefinition.rhythmHitColor : Color.yellow;
        text.text = comboText;
        text.fontStyle = TMPro.FontStyles.Bold;
        
        // Animate and destroy
        StartCoroutine(AnimateAndDestroyText(textObj));
    }
    
    /// <summary>
    /// Animate text feedback and destroy it
    /// /// </summary>
    private System.Collections.IEnumerator AnimateAndDestroyText(GameObject textObj)
    {
        RectTransform rectTransform = textObj.GetComponent<RectTransform>();
        TMPro.TextMeshProUGUI text = textObj.GetComponent<TMPro.TextMeshProUGUI>();
        
        float duration = 2.0f;
        float elapsed = 0f;
        
        // Initial scale effect
        rectTransform.localScale = Vector3.one * comboTextSize;
        
        while (elapsed < duration)
        {
            // Bounce effect
            float bounceY = Mathf.Sin(elapsed * 10f) * 5f;
            float baseY = Mathf.Lerp(100, 150, Mathf.Min(1, elapsed / (duration * 0.6f)));
            rectTransform.anchoredPosition = new Vector2(0, baseY + bounceY);
            
            // Scale pulse effect
            float scale = 1.0f + 0.1f * Mathf.Sin(elapsed * 8f);
            rectTransform.localScale = Vector3.one * scale * comboTextSize;
            
            // Fade out at the end
            if (elapsed > duration * 0.7f)
            {
                float alpha = Mathf.Lerp(1, 0, (elapsed - duration * 0.7f) / (duration * 0.3f));
                text.color = new Color(text.color.r, text.color.g, text.color.b, alpha);
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Destroy(textObj);
    }
    
    /// <summary>
    /// Try to populate with default actions if none are set
    /// </summary>
    private void PopulateDefaultComboSequence()
    {
        // Check if the NewFighter has attack data we can use
        if (attachedFighter == null) return;
        
        // Find actions directly from fighter
        // Note: We can't directly access private 'actions' list in NewFighter
        // So instead, we'll use Resources to find actions in the project
        ActionData[] availableActions = Resources.FindObjectsOfTypeAll<ActionData>();
        if (availableActions == null || availableActions.Length == 0) return;
        
        // Add default combo sequence: Punch → Punch → Throw
        // This is the sequence the user specifically requested
        ActionData punchAction = null;
        ActionData throwAction = null;
        
        // Find punch and throw actions
        foreach (ActionData action in availableActions)
        {
            if (action == null) continue;
            
            string actionNameLower = action.actionName.ToLower();
            if (actionNameLower.Contains("punch") && punchAction == null)
            {
                punchAction = action;
            }
            else if ((actionNameLower.Contains("throw") || actionNameLower.Contains("grab")) && throwAction == null)
            {
                throwAction = action;
            }
            
            // Break early if we found both
            if (punchAction != null && throwAction != null) break;
        }
        
        // Add the sequence: punch, punch, throw
        if (punchAction != null)
        {
            fallbackComboSequence.Add(punchAction); // First punch
            fallbackComboSequence.Add(punchAction); // Second punch
        }
        
        if (throwAction != null)
        {
            fallbackComboSequence.Add(throwAction); // Final throw
        }
        
        Debug.Log($"Populated default combo sequence with {fallbackComboSequence.Count} actions");
    }
    
    /// <summary>
    /// Override the default action input with combo-specific action if in a combo sequence
    /// </summary>
    /// <param name="defaultAction">Default action that would be performed</param>
    /// <returns>Modified action based on combo state</returns>
    public ActionData ModifyActionBasedOnCombo(ActionData defaultAction)
    {
        // Check if we have any actions to use for combo
        if (!inComboSequence || (comboActions.Count == 0 && fallbackComboSequence.Count == 0))
            return defaultAction;
            
        return GetCurrentComboAction();
    }
}
