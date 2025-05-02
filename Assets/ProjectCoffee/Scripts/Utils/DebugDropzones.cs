using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Utility script for debugging drop zone and draggable issues
/// Attach to any GameObject in your scene to monitor drag and drop operations
/// </summary>
public class DebugDropzones : MonoBehaviour
{
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool visualizeDropZones = true;
    [SerializeField] private Color dropZoneColor = new Color(0, 1, 0, 0.2f);
    
    private List<DropZone> allDropZones = new List<DropZone>();
    private List<Draggable> allDraggables = new List<Draggable>();
    private Dictionary<DropZone, Image> debugHighlights = new Dictionary<DropZone, Image>();
    
    private void Start()
    {
        // Find all drop zones and draggables in the scene
        allDropZones.AddRange(FindObjectsOfType<DropZone>());
        allDraggables.AddRange(FindObjectsOfType<Draggable>());
        
        if (enableDebugLogs)
        {
            Debug.Log($"Found {allDropZones.Count} drop zones and {allDraggables.Count} draggables in the scene.");
            
            // Log drop zone details
            foreach (var dropZone in allDropZones)
            {
                Debug.Log($"DropZone: {dropZone.name}, Active: {dropZone.gameObject.activeInHierarchy}, " +
                          $"Components: {string.Join(", ", GetComponentNames(dropZone.gameObject))}");
            }
            
            // Log draggable details
            foreach (var draggable in allDraggables)
            {
                Debug.Log($"Draggable: {draggable.name}, Return on fail: {draggable.GetComponent<Draggable>().returnToOriginalPositionOnFail}, " +
                          $"Components: {string.Join(", ", GetComponentNames(draggable.gameObject))}");
            }
        }
        
        if (visualizeDropZones)
        {
            CreateDropZoneHighlights();
        }
        
        // Monitor Draggable events by adding a component to existing draggables
        foreach (var draggable in allDraggables)
        {
            if (draggable.gameObject.GetComponent<DraggableDebugger>() == null)
            {
                draggable.gameObject.AddComponent<DraggableDebugger>();
            }
        }
    }
    
    private string[] GetComponentNames(GameObject obj)
    {
        Component[] components = obj.GetComponents<Component>();
        string[] names = new string[components.Length];
        
        for (int i = 0; i < components.Length; i++)
        {
            names[i] = components[i].GetType().Name;
        }
        
        return names;
    }
    
    private void CreateDropZoneHighlights()
    {
        foreach (var dropZone in allDropZones)
        {
            if (dropZone == null || !dropZone.gameObject.activeInHierarchy) continue;
            
            // Create a highlight visualization
            GameObject highlight = new GameObject("DropZoneHighlight");
            highlight.transform.SetParent(dropZone.transform);
            
            // Match the drop zone's rect transform
            RectTransform dzRect = dropZone.GetComponent<RectTransform>();
            RectTransform hlRect = highlight.AddComponent<RectTransform>();
            hlRect.anchorMin = dzRect.anchorMin;
            hlRect.anchorMax = dzRect.anchorMax;
            hlRect.offsetMin = dzRect.offsetMin;
            hlRect.offsetMax = dzRect.offsetMax;
            hlRect.anchoredPosition = Vector2.zero;
            hlRect.sizeDelta = dzRect.sizeDelta;
            
            // Add a colored image
            Image img = highlight.AddComponent<Image>();
            img.color = dropZoneColor;
            img.raycastTarget = false; // Don't block raycasts
            
            // Store for reference
            debugHighlights[dropZone] = img;
        }
    }
    
    private void OnDestroy()
    {
        // Clean up debug visualizations
        foreach (var highlight in debugHighlights.Values)
        {
            if (highlight != null)
            {
                Destroy(highlight.gameObject);
            }
        }
    }
}

/// <summary>
/// Helper component added to draggables for debugging
/// </summary>
public class DraggableDebugger : MonoBehaviour
{
    private Draggable draggable;
    private Vector3 startScale;
    
    private void Awake()
    {
        draggable = GetComponent<Draggable>();
        if (draggable == null)
        {
            Debug.LogError("DraggableDebugger attached to object without Draggable component");
            return;
        }
        
        startScale = transform.localScale;
    }
    
    private void OnEnable()
    {
        // Reset scale when enabled
        transform.localScale = startScale;
    }
    
    private void Update()
    {
        // Monitor scale changes
        if (transform.localScale != startScale)
        {
            Debug.Log($"Scale changed on {gameObject.name}: {transform.localScale} (original: {startScale})");
        }
    }
}