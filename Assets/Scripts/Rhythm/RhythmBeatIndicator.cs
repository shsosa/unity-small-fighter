using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RhythmBeatIndicator : MonoBehaviour
{
    public Image beatImage;
    public float pulseSpeed = 5f;
    public float maxScale = 1.5f;
    
    private void Start()
    {
        if (beatImage == null)
            beatImage = GetComponent<Image>();
            
        StartCoroutine(PulseAndFade());
    }
    
    private IEnumerator PulseAndFade()
    {
        float alpha = 1.0f;
        float scale = 0.5f;
        
        Color startColor = beatImage.color;
        
        while (alpha > 0)
        {
            // Pulse outward
            scale = Mathf.Min(scale + Time.deltaTime * pulseSpeed, maxScale);
            
            // Fade out
            alpha -= Time.deltaTime;
            
            // Apply changes
            beatImage.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            beatImage.transform.localScale = Vector3.one * scale;
            
            yield return null;
        }
        
        Destroy(gameObject);
    }
}
