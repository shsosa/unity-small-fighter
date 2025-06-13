using System.Collections;
using UnityEngine;

/// <summary>
/// Add this to any scene to clean up old rhythm components and set up the simple system
/// </summary>
public class SetupSimpleRhythm : MonoBehaviour
{
    public AudioClip musicClip;
    public float bpm = 120f;
    
    void Start()
    {
        StartCoroutine(CleanupAndSetup());
    }
    
    IEnumerator CleanupAndSetup()
    {
        Debug.Log("Starting cleanup of old rhythm systems...");
        
        // Remove old components that cause errors
        RemoveComponentsOfType<RhythmBootstrapper>();
        RemoveComponentsOfType<RhythmManager>();
        RemoveComponentsOfType<RhythmCombatStarter>();
        
        // Try to remove AudioVisualizer components using Type.GetType (in case they're not directly accessible)
        RemoveComponentsByName("AudioSampler");
        RemoveComponentsByName("AudioEventListener");
        
        yield return null;
        
        Debug.Log("Setting up SimpleRhythmSystem...");
        
        // Create a new GameObject for the simple rhythm system
        GameObject rhythmObj = new GameObject("SimpleRhythmSystem");
        SimpleRhythmBootstrapper bootstrapper = rhythmObj.AddComponent<SimpleRhythmBootstrapper>();
        
        // Configure it
        bootstrapper.musicClip = musicClip;
        bootstrapper.bpm = bpm;
        
        Debug.Log("SimpleRhythmSystem setup complete!");
        
        // Remove this setup script after it's done
        Destroy(this);
    }
    
    void RemoveComponentsOfType<T>() where T : Component
    {
        T[] components = FindObjectsOfType<T>();
        foreach (T component in components)
        {
            Debug.Log($"Removing {typeof(T).Name}: {component.name}");
            Destroy(component);
        }
    }
    
    void RemoveComponentsByName(string componentName)
    {
        MonoBehaviour[] allComponents = FindObjectsOfType<MonoBehaviour>();
        foreach (MonoBehaviour comp in allComponents)
        {
            if (comp.GetType().Name == componentName)
            {
                Debug.Log($"Removing {componentName}: {comp.name}");
                Destroy(comp);
            }
        }
    }
}
