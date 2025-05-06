using UnityEngine;

/// <summary>
/// Advanced drop zone specifically for portafilters
/// </summary>
public class PortafilterDropZone : DropZone
{
    [SerializeField] private CoffeeGrammingMachine parentMachine;
    
    private Portafilter currentPortafilter;
    
    private void Awake()
    {
        // CRITICAL: Set the accept predicate in Awake to override any settings from the inspector
        AcceptPredicate = (item) => item is Portafilter;
        base.LogDebug("PortafilterDropZone initialized: Accept predicate set");
    }
    
    private void Start()
    {
        // Double check that our accept predicate is properly set
        base.LogDebug($"PortafilterDropZone started. AcceptPredicate is set: {AcceptPredicate != null}");
    }
    
    public override bool CanAccept(Draggable item)
    {
        bool baseAccept = isActive; // Skip the base.CanAccept check which causes problems
        bool isPortafilter = item is Portafilter;
        
        base.LogDebug($"CanAccept check for {item?.name}: isActive={isActive}, isPortafilter={isPortafilter}");
        
        return isActive && isPortafilter;
    }
    
    public override void OnItemDropped(Draggable item)
    {
        // Don't call base because it's causing issues with the hierarchy change
        // Instead, implement custom placement logic
        
        if (item == null) return;
        
        base.LogDebug($"OnItemDropped: Handling {item.name}");
        
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
        
        base.LogDebug($"Portafilter {item.name} successfully placed in drop zone");
        
        // Notify the machine
        if (parentMachine != null)
        {
            base.LogDebug($"Notifying machine that portafilter was placed");
            parentMachine.OnPortafilterDropped(item);
        }
        else
        {
            base.LogDebug("WARNING: parentMachine reference is null, cannot notify");
        }
    }
    
    // This is called when a child is removed manually (by drag)
    private void OnTransformChildrenChanged()
    {
        if (transform.childCount == 0 && currentPortafilter != null && parentMachine != null)
        {
            base.LogDebug($"Portafilter {currentPortafilter.name} was removed");
            
            // Notify the machine
            parentMachine.OnPortafilterRemoved(currentPortafilter);
            
            // Clear our reference
            currentPortafilter = null;
        }
    }
    
    // Remove the custom LogDebugPortafilter method entirely since we use base.LogDebug
}