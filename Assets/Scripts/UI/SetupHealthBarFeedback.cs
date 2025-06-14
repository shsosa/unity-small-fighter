using UnityEngine;

/// <summary>
/// Automatically sets up the health bar feedback system in the scene
/// </summary>
[DefaultExecutionOrder(-50)] // Execute early
public class SetupHealthBarFeedback : MonoBehaviour
{
    private void Awake()
    {
        // Check if components already exist
        if (FindObjectOfType<SimpleRhythmHitDetector>() == null)
        {
            // Create components on this object if missing
            gameObject.AddComponent<SimpleRhythmHitDetector>();
            Debug.Log("Added SimpleRhythmHitDetector component");
        }
        
        if (FindObjectOfType<HealthBarFeedbackManager>() == null)
        {
            gameObject.AddComponent<HealthBarFeedbackManager>();
            Debug.Log("Added HealthBarFeedbackManager component");
        }
    }
}
