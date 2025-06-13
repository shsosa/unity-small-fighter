using System.Collections;
using UnityEngine;

public class RhythmHitEffect : MonoBehaviour
{
    public float duration = 1.0f;
    public float growSpeed = 3.0f;
    public float maxScale = 2.0f;
    public Color startColor = Color.yellow;
    public Color endColor = new Color(1, 1, 0, 0);
    
    private SpriteRenderer spriteRenderer;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        StartCoroutine(AnimateEffect());
    }
    
    IEnumerator AnimateEffect()
    {
        float elapsedTime = 0f;
        transform.localScale = Vector3.one * 0.5f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            
            // Grow the effect
            float scale = Mathf.Min(transform.localScale.x + Time.deltaTime * growSpeed, maxScale);
            transform.localScale = Vector3.one * scale;
            
            // Fade out
            float t = elapsedTime / duration;
            spriteRenderer.color = Color.Lerp(startColor, endColor, t);
            
            yield return null;
        }
        
        Destroy(gameObject);
    }
}
