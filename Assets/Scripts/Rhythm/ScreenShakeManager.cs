using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles screen shake effects for rhythm hits
/// </summary>
public class ScreenShakeManager : MonoBehaviour
{
    public Canvas targetCanvas;
    
    private Vector3 originalPosition;
    private RectTransform canvasRect;
    private Coroutine currentShakeRoutine;
    
    private void Awake()
    {
        if (targetCanvas == null)
        {
            targetCanvas = FindObjectOfType<Canvas>();
        }
        
        if (targetCanvas != null)
        {
            canvasRect = targetCanvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                originalPosition = canvasRect.anchoredPosition;
            }
        }
    }
    
    /// <summary>
    /// Shake the screen with the specified parameters
    /// </summary>
    /// <param name="duration">Duration of the shake</param>
    /// <param name="intensity">Intensity of the shake (how far it moves)</param>
    public IEnumerator ShakeRoutine(float duration, float intensity)
    {
        // Early out if we don't have a canvas
        if (canvasRect == null)
            yield break;
        
        // Stop any ongoing shake
        if (currentShakeRoutine != null)
        {
            StopCoroutine(currentShakeRoutine);
            canvasRect.anchoredPosition = originalPosition;
        }
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            // Calculate random shake offset
            float offsetX = Random.Range(-intensity, intensity) * 100f;
            float offsetY = Random.Range(-intensity, intensity) * 100f;
            
            // Apply shake
            canvasRect.anchoredPosition = originalPosition + new Vector3(offsetX, offsetY, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we end at the original position
        canvasRect.anchoredPosition = originalPosition;
    }
    
    /// <summary>
    /// Shake the camera with the specified parameters
    /// </summary>
    /// <param name="duration">Duration of the shake</param>
    /// <param name="intensity">Intensity of the shake (how far it moves)</param>
    public void Shake(float duration, float intensity)
    {
        if (currentShakeRoutine != null)
        {
            StopCoroutine(currentShakeRoutine);
        }
        
        currentShakeRoutine = StartCoroutine(ShakeRoutine(duration, intensity));
    }
}
