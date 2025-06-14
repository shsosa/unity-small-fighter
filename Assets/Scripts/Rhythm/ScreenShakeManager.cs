using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles screen shake effects for rhythm hits
/// </summary>
public class ScreenShakeManager : MonoBehaviour
{
    public Canvas targetCanvas;
    private Camera targetCamera;
    
    private Vector3 originalCanvasPosition;
    private Vector3 originalCameraPosition;
    private RectTransform canvasRect;
    private Coroutine currentShakeRoutine;
    
    // Static reference for easy access
    private static ScreenShakeManager _instance;
    
    private void Awake()
    {
        // Set up singleton instance
        _instance = this;
        
        // Find canvas if not assigned
        if (targetCanvas == null)
        {
            targetCanvas = FindObjectOfType<Canvas>();
        }
        
        // Set up canvas shake
        if (targetCanvas != null)
        {
            canvasRect = targetCanvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                originalCanvasPosition = canvasRect.anchoredPosition;
            }
        }
        
        // Set up camera shake
        targetCamera = Camera.main;
        if (targetCamera != null)
        {
            originalCameraPosition = targetCamera.transform.localPosition;
        }
        
        Debug.Log("ScreenShakeManager initialized");
    }
    
    /// <summary>
    /// Shake the screen with the specified parameters
    /// </summary>
    /// <param name="duration">Duration of the shake</param>
    /// <param name="intensity">Intensity of the shake (how far it moves)</param>
    public IEnumerator ShakeRoutine(float duration, float intensity)
    {
        Debug.Log($"Starting screen shake: {duration}s at {intensity} intensity");
        
        // Stop any ongoing shake
        if (currentShakeRoutine != null)
        {
            StopCoroutine(currentShakeRoutine);
            ResetPositions();
        }
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            // Calculate random shake offset
            float offsetX = Random.Range(-intensity, intensity) * 100f;
            float offsetY = Random.Range(-intensity, intensity) * 100f;
            
            // Apply shake to canvas if available
            if (canvasRect != null)
            {
                canvasRect.anchoredPosition = originalCanvasPosition + new Vector3(offsetX, offsetY, 0);
            }
            
            // Apply shake to camera if available
            if (targetCamera != null)
            {
                targetCamera.transform.localPosition = originalCameraPosition + new Vector3(offsetX/200f, offsetY/200f, 0);
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we end at the original position
        ResetPositions();
        
        Debug.Log("Screen shake complete");
    }
    
    /// <summary>
    /// Shake the camera with the specified parameters
    /// </summary>
    /// <param name="duration">Duration of the shake</param>
    /// <param name="intensity">Intensity of the shake (how far it moves)</param>
    public void Shake(float duration, float intensity)
    {
        Debug.Log($"Shake called with duration={duration}, intensity={intensity}");
        if (currentShakeRoutine != null)
        {
            StopCoroutine(currentShakeRoutine);
        }
        
        currentShakeRoutine = StartCoroutine(ShakeRoutine(duration, intensity));
    }
    
    /// <summary>
    /// Static method to shake the screen from anywhere
    /// </summary>
    public static void ShakeScreen(float duration, float intensity)
    {
        // Create instance if none exists
        if (_instance == null)
        {
            GameObject shakerObj = new GameObject("ScreenShakeManager");
            _instance = shakerObj.AddComponent<ScreenShakeManager>();
            Debug.Log("Created new screen shake manager");
        }
        
        _instance.Shake(duration, intensity);
    }
    
    private void ResetPositions()
    {
        // Reset canvas position
        if (canvasRect != null)
        {
            canvasRect.anchoredPosition = originalCanvasPosition;
        }
        
        // Reset camera position
        if (targetCamera != null)
        {
            targetCamera.transform.localPosition = originalCameraPosition;
        }
    }
}
