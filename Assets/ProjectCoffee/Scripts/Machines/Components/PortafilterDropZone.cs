using UnityEngine;

/// <summary>
/// Advanced drop zone specifically for portafilters
/// </summary>
public class PortafilterDropZone : DropZone
{
    [SerializeField] private CoffeeGrammingMachine parentMachine;
    [SerializeField] private bool debugLogs = true;
    
    private Portafilter currentPortafilter;
    
    private void Awake()
    {
        // CRITICAL: Set the accept predicate in Awake to override any settings from the inspector
        AcceptPredicate = (item) => item is Portafilter;
        LogDebug("PortafilterDropZone initialized: Accept predicate set");
    }
    
    private void Start()
    {
        // Double check that our accept predicate is properly set
        LogDebug($"PortafilterDropZone started. AcceptPredicate is set: {AcceptPredicate != null}");
    }
    
    public override bool CanAccept(Draggable item)
    {
        bool baseAccept = isActive; // Skip the base.CanAccept check which causes problems
        bool isPortafilter = item is Portafilter;
        
        LogDebug($"CanAccept check for {item?.name}: isActive={isActive}, isPortafilter={isPortafilter}");
        
        return isActive && isPortafilter;
    }
    
    public override void OnItemDropped(Draggable item)
    {
        // Don't call base because it's causing issues with the hierarchy change
        // Instead, implement custom placement logic
        
        if (item == null) return;
        
        LogDebug($"OnItemDropped: Handling {item.name}");
        
        RectTransform itemRect = item.GetComponent<RectTransform>();
        if (itemRect == null) return;
        
        // Store original scale
        Vector3 originalScale = item.transform.localScale;
        
        // Set the portafilter as our child
        itemRect.SetParent(transform);
        itemRect.anchoredPosition = Vector2.zero;
        
        // Restore original scale
        itemRect.localScale = originalScale;
        
        // Track the current portafilter
        currentPortafilter = item as Portafilter;
        
        LogDebug($"Portafilter {item.name} successfully placed in drop zone");
        
        // Notify the machine
        if (parentMachine != null)
        {
            LogDebug($"Notifying machine that portafilter was placed");
            parentMachine.OnPortafilterDropped(item);
        }
        else
        {
            LogDebug("WARNING: parentMachine reference is null, cannot notify");
        }
    }
    
    // This is called when a child is removed manually (by drag)
    private void OnTransformChildrenChanged()
    {
        if (transform.childCount == 0 && currentPortafilter != null && parentMachine != null)
        {
            LogDebug($"Portafilter {currentPortafilter.name} was removed");
            
            // Notify the machine
            parentMachine.OnPortafilterRemoved(currentPortafilter);
            
            // Clear our reference
            currentPortafilter = null;
        }
    }
    
    private void LogDebug(string message)
    {
        if (debugLogs)
        {
            Debug.Log($"[PortafilterDropZone] {message}");
        }
    }
}