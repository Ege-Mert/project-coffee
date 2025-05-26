using UnityEngine;
using ProjectCoffee.Interaction.Helpers;

/// <summary>
/// Advanced drop zone specifically for portafilters (updated with item tracking)
/// </summary>
public class PortafilterDropZone : DropZone
{
    [SerializeField] private CoffeeGrammingMachine parentMachine;
    
    private Portafilter currentPortafilter;
    private DropZoneItemTracker itemTracker;
    
    protected virtual void Start()
    {
        // base.Awake();
        
        // Add item tracker if not present
        itemTracker = GetComponent<DropZoneItemTracker>();
        if (itemTracker == null)
        {
            itemTracker = gameObject.AddComponent<DropZoneItemTracker>();
        }
        
        // Set the accept predicate
        AcceptPredicate = (item) => item is Portafilter;
        LogDebug("PortafilterDropZone initialized with item tracker");
    }
    
    public override bool CanAccept(Draggable item)
    {
        // First check with tracker to prevent overlapping
        if (!itemTracker.CanAcceptItem(item))
        {
            LogDebug($"Item tracker rejected {item.name} - zone already has an item");
            return false;
        }
        
        bool baseAccept = isActive;
        bool isPortafilter = item is Portafilter;
        
        LogDebug($"CanAccept check for {item?.name}: isActive={isActive}, isPortafilter={isPortafilter}");
        
        return isActive && isPortafilter;
    }
    
    public override void OnItemDropped(Draggable item)
    {
        if (item == null) return;
        
        LogDebug($"OnItemDropped: Handling {item.name}");
        
        // Update tracker first
        itemTracker.SetItem(item);
        
        // Ensure item has state manager for processing states
        var stateManager = item.GetComponent<DraggableStateManager>();
        if (stateManager == null)
        {
            stateManager = item.gameObject.AddComponent<DraggableStateManager>();
        }
        
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
        if (transform.childCount == 0 && currentPortafilter != null)
        {
            LogDebug($"Portafilter {currentPortafilter.name} was removed");
            
            // Clear tracker
            itemTracker.ClearItem();
            
            // Notify the machine
            if (parentMachine != null)
            {
                parentMachine.OnPortafilterRemoved(currentPortafilter);
            }
            
            // Clear our reference
            currentPortafilter = null;
        }
    }
}
