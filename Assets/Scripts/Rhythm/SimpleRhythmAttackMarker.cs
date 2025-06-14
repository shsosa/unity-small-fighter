using UnityEngine;

/// <summary>
/// Marker component to track rhythm attacks and their damage multipliers
/// </summary>
public class SimpleRhythmAttackMarker : MonoBehaviour
{
    // When the attack was initiated
    public float timeOfAttack;
    
    // The damage multiplier to apply
    public float damageMultiplier = 1.0f;
    
    // Whether the attack was on beat or not
    public bool wasOnBeat = false;
}
