using UnityEngine;

/// <summary>
/// Connects the rhythm UI system to the existing rhythm combat system
/// </summary>
public class RhythmUIConnector : MonoBehaviour
{
    public RhythmLaneUI laneUI;
    public SimpleRhythmFighter fighter;
    public SimpleRhythmSystem rhythmSystem;
    
    private void Start()
    {
        // Find components if not assigned
        if (laneUI == null)
        {
            laneUI = FindObjectOfType<RhythmLaneUI>();
            if (laneUI == null)
            {
                // Create the lane UI if it doesn't exist
                GameObject laneObject = new GameObject("RhythmLaneUI");
                laneUI = laneObject.AddComponent<RhythmLaneUI>();
                Debug.Log("Created RhythmLaneUI");
            }
        }
        
        if (fighter == null)
        {
            fighter = GetComponent<SimpleRhythmFighter>();
        }
        
        if (rhythmSystem == null)
        {
            rhythmSystem = FindObjectOfType<SimpleRhythmSystem>();
        }
        
        // Connect events
        if (fighter != null)
        {
            fighter.OnPerfectHit += OnRhythmHit;
            fighter.OnMissedBeat += OnRhythmMiss;
        }
    }
    
    private void OnDestroy()
    {
        // Disconnect events
        if (fighter != null)
        {
            fighter.OnPerfectHit -= OnRhythmHit;
            fighter.OnMissedBeat -= OnRhythmMiss;
        }
    }
    
    private void OnRhythmHit()
    {
        if (laneUI != null)
        {
            laneUI.FlashHitZone(true);
        }
    }
    
    private void OnRhythmMiss()
    {
        if (laneUI != null)
        {
            laneUI.FlashHitZone(false);
        }
    }
    
    /// <summary>
    /// Add this component to a fighter to enable rhythm UI
    /// </summary>
    /// <param name="fighter">The fighter to add UI to</param>
    public static void AddToFighter(SimpleRhythmFighter fighter)
    {
        if (fighter == null) return;
        
        // Add the connector component
        fighter.gameObject.AddComponent<RhythmUIConnector>();
    }
}
