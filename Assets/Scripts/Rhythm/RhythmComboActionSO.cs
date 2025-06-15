using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scriptable Object to define rhythm combo sequences
/// </summary>
[CreateAssetMenu(fileName = "New Rhythm Combo", menuName = "Rhythm Fighter/Rhythm Combo")]
public class RhythmComboActionSO : ScriptableObject
{
    [System.Serializable]
    public class ComboAction
    {
        public string actionName;
        public ActionData action;
        [Tooltip("How many successful rhythm hits needed to progress to this action")]
        public int hitsToProgress = 1;
    }

    [Header("Combo Sequence")]
    [Tooltip("List of actions to sequence through when hitting on rhythm")]
    public List<ComboAction> comboSequence = new List<ComboAction>();
    
    [Header("Combo Settings")]
    [Tooltip("Reset combo if player misses this many beats")]
    public int missesToReset = 2;
    
    [Header("Feedback")]
    public Color normalHitColor = Color.white;
    public Color rhythmHitColor = Color.yellow;
}
