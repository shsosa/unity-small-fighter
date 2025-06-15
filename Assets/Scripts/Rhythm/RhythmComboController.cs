using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls rhythm-based combo sequences that change attacks based on successful rhythm hits.
/// When the player hits the same button on rhythm, this controller will cycle through different attacks.
/// </summary>
public class RhythmComboController : MonoBehaviour
{
    [Header("Combo Settings")]
    [Tooltip("List of actions to sequence through when hitting on rhythm")]
    [SerializeField] private List<ActionData> comboSequence = new List<ActionData>();
    
    [Tooltip("How many successful rhythm hits are needed to progress to next combo")]
    [SerializeField] private int hitsToProgress = 1;
    
    [Tooltip("Reset combo if player misses this many beats")]
    [SerializeField] private int missesToReset = 2;
    
    [Header("Feedback")]
    [SerializeField] private Color normalHitColor = Color.white;
    [SerializeField] private Color rhythmHitColor = Color.yellow;
    [SerializeField] private float comboTextSize = 1.5f;
    
    // Current state
    private int currentComboIndex = 0;
    private int successfulHits = 0;
    private int missedHits = 0;
    private NewFighter attachedFighter;
    private SimpleRhythmFighter rhythmFighter;
    
    // Track if we're already in a combo
    private bool inComboSequence = false;
    
    void Start()
    {
        // Get the fighter components
        attachedFighter = GetComponent<NewFighter>();
        rhythmFighter = GetComponent<SimpleRhythmFighter>();
        
        if (rhythmFighter == null)
        {
            Debug.LogError("RhythmComboController requires a SimpleRhythmFighter component");
            return;
        }
        
        // Subscribe to rhythm hit events
        rhythmFighter.OnRhythmHit.AddListener(OnRhythmHitDetected);
        rhythmFighter.OnRhythmMiss.AddListener(OnRhythmMissDetected);
        
        // If no combo sequence is set, look for default actions
        if (comboSequence.Count == 0)
        {
            PopulateDefaultComboSequence();
        }
    }
    
    /// <summary>
    /// Called when a rhythm hit is detected
    /// </summary>
    public void OnRhythmHitDetected()
    {
        successfulHits++;
        missedHits = 0;
        
        // Check if we have enough hits to progress
        if (successfulHits >= hitsToProgress)
        {
            // Progress combo
            ProgressCombo();
            successfulHits = 0;
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
    /// Progress to the next attack in the combo sequence
    /// </summary>
    private void ProgressCombo()
    {
        if (comboSequence.Count == 0) return;
        
        // Increment combo index and wrap around
        currentComboIndex = (currentComboIndex + 1) % comboSequence.Count;
        inComboSequence = true;
        
        // Feedback for new combo level
        Debug.Log($"Combo progressed to: {comboSequence[currentComboIndex].actionName}");
    }
    
    /// <summary>
    /// Reset the combo sequence
    /// </summary>
    private void ResetCombo()
    {
        currentComboIndex = 0;
        successfulHits = 0;
        missedHits = 0;
        inComboSequence = false;
        
        Debug.Log("Combo reset");
    }
    
    /// <summary>
    /// Get the current action in the sequence based on combo progress
    /// </summary>
    public ActionData GetCurrentComboAction()
    {
        if (comboSequence.Count == 0) return null;
        
        return comboSequence[currentComboIndex];
    }
    
    /// <summary>
    /// Show combo status feedback on screen
    /// </summary>
    private void ShowComboStatus()
    {
        if (comboSequence.Count == 0) return;
        
        // Create a text message showing the current combo level and next attack
        string comboText = $"COMBO LEVEL {currentComboIndex + 1}: {comboSequence[currentComboIndex].actionName}";
        
        // Display the message using the UI system
        // (This would be expanded with proper UI for a rhythm game feel)
        GameObject textObj = new GameObject("ComboStatusText");
        textObj.transform.SetParent(GameObject.Find("Canvas").transform);
        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        
        // Position at the bottom of the screen
        rectTransform.anchorMin = new Vector2(0.5f, 0);
        rectTransform.anchorMax = new Vector2(0.5f, 0);
        rectTransform.pivot = new Vector2(0.5f, 0);
        rectTransform.anchoredPosition = new Vector2(0, 100);
        
        // Add text component
        UnityEngine.UI.Text text = textObj.AddComponent<UnityEngine.UI.Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 24;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = rhythmHitColor;
        text.text = comboText;
        
        // Animate and destroy
        StartCoroutine(AnimateAndDestroyText(textObj));
    }
    
    /// <summary>
    /// Animate text feedback and destroy it
    /// </summary>
    private System.Collections.IEnumerator AnimateAndDestroyText(GameObject textObj)
    {
        RectTransform rectTransform = textObj.GetComponent<RectTransform>();
        UnityEngine.UI.Text text = textObj.GetComponent<UnityEngine.UI.Text>();
        
        float duration = 1.0f;
        float elapsed = 0f;
        
        // Initial scale effect
        rectTransform.localScale = Vector3.one * comboTextSize;
        
        while (elapsed < duration)
        {
            // Move up
            rectTransform.anchoredPosition += new Vector2(0, 40 * Time.deltaTime);
            
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
        // Since we can't directly access the fighter's action list, we'll create default actions
        // Look for common action assets in the Resources folder
        ActionData[] actionAssets = Resources.FindObjectsOfTypeAll<ActionData>();
        
        if (actionAssets != null && actionAssets.Length > 0) 
        {
            // Add up to 3 of these actions to our sequence
            int count = Mathf.Min(3, actionAssets.Length);
            for (int i = 0; i < count; i++)
            {
                comboSequence.Add(actionAssets[i]);
                Debug.Log($"Added action to combo sequence: {actionAssets[i].actionName}");
            }
        }
        else 
        {
            Debug.LogWarning("No actions found for combo sequence. Please assign them in the inspector.");
        }
    }
    
    /// <summary>
    /// Override the default action input with combo-specific action if in a combo sequence
    /// </summary>
    /// <param name="defaultAction">Default action that would be performed</param>
    /// <returns>Modified action based on combo state</returns>
    public ActionData ModifyActionBasedOnCombo(ActionData defaultAction)
    {
        if (!inComboSequence || comboSequence.Count == 0) 
            return defaultAction;
            
        return GetCurrentComboAction();
    }
}
