using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates a rhythm game lane UI with scrolling notes at the bottom of the screen
/// </summary>
public class RhythmLaneUI : MonoBehaviour
{
    [Header("Lane Settings")]
    [SerializeField] private int numberOfLanes = 1;
    [SerializeField] private float laneHeight = 80f;
    [SerializeField] private float laneWidth = 800f;
    [SerializeField] private Color laneColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    [SerializeField] private Color laneDividerColor = new Color(1f, 1f, 0f, 0.7f); // Yellow dividers
    
    [Header("Note Settings")]
    [SerializeField] private float noteSpeed = 200f; // Units per second
    [SerializeField] private float noteSize = 40f;
    [SerializeField] private Color normalNoteColor = Color.white;
    [SerializeField] private Color perfectNoteColor = Color.yellow;
    [SerializeField] private Color missedNoteColor = Color.red;
    
    [Header("Hit Zone")]
    [SerializeField] private float hitZoneSize = 50f;
    [SerializeField] private Color hitZoneColor = new Color(0f, 1f, 0f, 0.7f); // Green hit zone
    
    // References
    private RectTransform laneContainer;
    private RectTransform hitZone;
    private List<RectTransform> lanes = new List<RectTransform>();
    private Dictionary<float, RectTransform> activeNotes = new Dictionary<float, RectTransform>();
    
    // Beat tracking
    private float secondsPerBeat;
    private float beatsToShow = 4f; // How many beats ahead to show notes
    private float beatProgress = 0f;
    private SimpleRhythmSystem rhythmSystem;
    
    private void Awake()
    {
        // Find or create the Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Create lane container
        GameObject containerObj = new GameObject("RhythmLaneContainer");
        containerObj.transform.SetParent(canvas.transform, false);
        laneContainer = containerObj.AddComponent<RectTransform>();
        
        // Position at bottom of screen
        laneContainer.anchorMin = new Vector2(0.5f, 0);
        laneContainer.anchorMax = new Vector2(0.5f, 0);
        laneContainer.pivot = new Vector2(0.5f, 0);
        laneContainer.sizeDelta = new Vector2(laneWidth, laneHeight);
        laneContainer.anchoredPosition = new Vector2(0, 20); // Small offset from bottom
        
        // Create background
        GameObject bgObj = new GameObject("LaneBackground");
        bgObj.transform.SetParent(laneContainer, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = laneColor;
        
        // Create hit zone
        GameObject hitZoneObj = new GameObject("HitZone");
        hitZoneObj.transform.SetParent(laneContainer, false);
        hitZone = hitZoneObj.AddComponent<RectTransform>();
        hitZone.anchorMin = new Vector2(0, 0);
        hitZone.anchorMax = new Vector2(1, 1);
        hitZone.pivot = new Vector2(0.5f, 0.5f);
        hitZone.sizeDelta = new Vector2(hitZoneSize, 0);
        hitZone.anchoredPosition = Vector2.zero; // Center
        
        Image hitZoneImage = hitZoneObj.AddComponent<Image>();
        hitZoneImage.color = hitZoneColor;
        
        // Create lanes
        CreateLanes();
        
        // Find the rhythm system
        rhythmSystem = FindObjectOfType<SimpleRhythmSystem>();
        if (rhythmSystem != null)
        {
            secondsPerBeat = 60f / rhythmSystem.bpm;
        }
        else
        {
            // Default to 120 BPM if no rhythm system found
            secondsPerBeat = 60f / 120f;
            Debug.LogWarning("No SimpleRhythmSystem found, defaulting to 120 BPM");
        }
    }
    
    private void CreateLanes()
    {
        float laneSize = laneWidth / numberOfLanes;
        
        for (int i = 0; i < numberOfLanes; i++)
        {
            // Create lane dividers
            if (i > 0)
            {
                GameObject dividerObj = new GameObject($"LaneDivider_{i}");
                dividerObj.transform.SetParent(laneContainer, false);
                RectTransform dividerRect = dividerObj.AddComponent<RectTransform>();
                
                dividerRect.anchorMin = new Vector2((float)i / numberOfLanes, 0);
                dividerRect.anchorMax = new Vector2((float)i / numberOfLanes, 1);
                dividerRect.sizeDelta = new Vector2(2, 0); // 2px wide divider
                
                Image dividerImage = dividerObj.AddComponent<Image>();
                dividerImage.color = laneDividerColor;
            }
            
            // Track lanes for note placement
            RectTransform laneRect = new GameObject($"Lane_{i}").AddComponent<RectTransform>();
            laneRect.SetParent(laneContainer, false);
            laneRect.anchorMin = new Vector2((float)i / numberOfLanes, 0);
            laneRect.anchorMax = new Vector2((float)(i + 1) / numberOfLanes, 1);
            laneRect.sizeDelta = Vector2.zero;
            
            lanes.Add(laneRect);
        }
    }
    
    private void Update()
    {
        // If no rhythm system found yet, try to find it
        if (rhythmSystem == null)
        {
            rhythmSystem = FindObjectOfType<SimpleRhythmSystem>();
            if (rhythmSystem != null)
            {
                secondsPerBeat = 60f / rhythmSystem.bpm;
            }
            return;
        }
        
        // Update beat progress
        beatProgress += Time.deltaTime / secondsPerBeat;
        
        // Generate new notes on beats
        if (beatProgress >= 1.0f)
        {
            beatProgress -= 1.0f;
            // Use the current time as an approximation for beat number
            float currentBeat = Time.time / secondsPerBeat;
            SpawnNoteOnBeat(currentBeat + beatsToShow);
        }
        
        // Move existing notes
        MoveNotes();
        
        // Check for hits and misses
        CheckForHitsAndMisses();
    }
    
    private void SpawnNoteOnBeat(float beatNumber)
    {
        // Select a random lane for now (can be improved to match actual gameplay)
        int laneIndex = Random.Range(0, lanes.Count);
        RectTransform lane = lanes[laneIndex];
        
        // Create note object
        GameObject noteObj = new GameObject($"Note_Beat{beatNumber}");
        noteObj.transform.SetParent(lane, false);
        RectTransform noteRect = noteObj.AddComponent<RectTransform>();
        
        // Position at right side of lane
        noteRect.anchorMin = new Vector2(1, 0.5f);
        noteRect.anchorMax = new Vector2(1, 0.5f);
        noteRect.pivot = new Vector2(0.5f, 0.5f);
        noteRect.sizeDelta = new Vector2(noteSize, noteSize);
        noteRect.anchoredPosition = Vector2.zero; // Start at right edge
        
        // Add visuals
        Image noteImage = noteObj.AddComponent<Image>();
        noteImage.color = normalNoteColor;
        
        // Use a circle sprite if available, otherwise use default
        Sprite circleSprite = Resources.Load<Sprite>("UI/Circle");
        if (circleSprite != null)
        {
            noteImage.sprite = circleSprite;
        }
        
        // Store the note with its target beat time
        activeNotes[beatNumber] = noteRect;
    }
    
    private void MoveNotes()
    {
        // Calculate time for a note to travel from right to hit zone
        float totalTravelTime = beatsToShow * secondsPerBeat;
        float distancePerSecond = laneWidth / totalTravelTime;
        
        // Current beat number (approximation)
        float currentBeat = Time.time / secondsPerBeat;
        
        // Move all active notes
        List<float> notesToRemove = new List<float>();
        foreach (var kvp in activeNotes)
        {
            float noteBeat = kvp.Key;
            RectTransform noteRect = kvp.Value;
            
            if (noteRect == null)
            {
                notesToRemove.Add(noteBeat);
                continue;
            }
            
            // Calculate position based on beat difference
            float beatDiff = noteBeat - currentBeat;
            float xPosition = -(laneWidth / 2) + (beatDiff / beatsToShow) * laneWidth;
            
            // Update position
            noteRect.anchoredPosition = new Vector2(xPosition, 0);
            
            // Remove notes that are way past the hit zone (missed)
            if (beatDiff < -0.5f)
            {
                notesToRemove.Add(noteBeat);
                Destroy(noteRect.gameObject);
            }
        }
        
        // Clean up removed notes
        foreach (float beat in notesToRemove)
        {
            activeNotes.Remove(beat);
        }
    }
    
    private void CheckForHitsAndMisses()
    {
        // Approximate current beat number
        float currentBeat = Time.time / secondsPerBeat;
        
        // Check for hits when player presses attack button
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.N)) // Player 1 or 2 attack keys
        {
            bool hitSuccess = false;
            float closestBeatDiff = float.MaxValue;
            float closestBeat = 0;
            
            // Find the closest note to hit zone
            foreach (var kvp in activeNotes)
            {
                float noteBeat = kvp.Key;
                RectTransform noteRect = kvp.Value;
                
                if (noteRect == null) continue;
                
                float beatDiff = Mathf.Abs(noteBeat - currentBeat);
                if (beatDiff < closestBeatDiff && beatDiff < 0.25f) // Within 1/4 beat window
                {
                    closestBeatDiff = beatDiff;
                    closestBeat = noteBeat;
                    hitSuccess = true;
                }
            }
            
            if (hitSuccess && activeNotes.TryGetValue(closestBeat, out RectTransform hitNoteRect))
            {
                // Visual feedback for hit
                Image noteImage = hitNoteRect.GetComponent<Image>();
                if (noteImage != null)
                {
                    if (closestBeatDiff < 0.1f) // Perfect hit (within 1/10 beat)
                    {
                        noteImage.color = perfectNoteColor;
                        hitNoteRect.sizeDelta *= 1.5f; // Make it bigger
                    }
                    else
                    {
                        noteImage.color = normalNoteColor;
                    }
                    
                    // Start fade out animation
                    StartCoroutine(FadeOutAndDestroy(hitNoteRect.gameObject));
                }
                
                // Remove from active notes
                activeNotes.Remove(closestBeat);
            }
        }
    }
    
    private IEnumerator FadeOutAndDestroy(GameObject obj)
    {
        Image img = obj.GetComponent<Image>();
        float duration = 0.2f;
        float elapsed = 0;
        Color startColor = img.color;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            img.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(1, 0, t));
            obj.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.5f, t);
            yield return null;
        }
        
        Destroy(obj);
    }
    
    /// <summary>
    /// Flash the hit zone to indicate a successful rhythm hit
    /// </summary>
    public void FlashHitZone(bool perfectHit)
    {
        if (hitZone == null) return;
        
        Image hitZoneImage = hitZone.GetComponent<Image>();
        if (hitZoneImage != null)
        {
            Color flashColor = perfectHit ? perfectNoteColor : normalNoteColor;
            StartCoroutine(FlashImage(hitZoneImage, flashColor));
        }
    }
    
    private IEnumerator FlashImage(Image img, Color flashColor)
    {
        Color originalColor = img.color;
        img.color = flashColor;
        
        float duration = 0.1f;
        float elapsed = 0;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            img.color = Color.Lerp(flashColor, originalColor, t);
            yield return null;
        }
        
        img.color = originalColor;
    }
    
    /// <summary>
    /// Adjust the active lanes based on the combo level
    /// </summary>
    public void UpdateForComboLevel(int comboLevel)
    {
        // We could use this to adjust lane visuals based on combo
        // For example, add more lanes or change colors as combo increases
    }
}
