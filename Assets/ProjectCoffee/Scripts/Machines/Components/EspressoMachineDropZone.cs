using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Helper class for espresso machine drop zones
/// </summary>
public class EspressoMachineDropZone : DropZone
{
    [SerializeField] private EspressoMachine parentMachine;
    [SerializeField] private int slotIndex;
    [SerializeField] private bool isPortafilterZone; // True for portafilter, false for cup
    [SerializeField] private bool debugLogs = true;
    
    private void Awake()
    {
        // Force set our accept predicate
        AcceptPredicate = (item) => {
            if (isPortafilterZone)
            {
                return item is Portafilter;
            }
            else
            {
                return item is Cup;
            }
        };
        
        LogDebug($"DropZone initialized: isPortafilterZone={isPortafilterZone}");
    }
    
    public override bool CanAccept(Draggable item)
    {
        bool baseActive = isActive; // Skip the base which might cause issues
        
        bool canAccept;
        if (isPortafilterZone)
        {
            canAccept = item is Portafilter;
        }
        else
        {
            canAccept = item is Cup;
        }
        
        LogDebug($"CanAccept check for {item?.name}: isActive={isActive}, canAccept={canAccept}");
        
        return isActive && canAccept;
    }
    
    public override void OnItemDropped(Draggable item)
    {
        LogDebug($"Item {item.name} dropped on zone");
        
        // Store original scale
        Vector3 originalScale = item.transform.localScale;
        
        // Set as child and position at center
        RectTransform itemRect = item.GetComponent<RectTransform>();
        itemRect.SetParent(transform);
        itemRect.anchoredPosition = Vector2.zero;
        
        // Restore original scale
        itemRect.localScale = originalScale;
        
        if (parentMachine != null)
        {
            if (isPortafilterZone && item is Portafilter)
            {
                LogDebug($"Notifying espresso machine of portafilter at slot {slotIndex}");
                parentMachine.OnPortafilterDropped(slotIndex, item);
            }
            else if (!isPortafilterZone && item is Cup)
            {
                LogDebug($"Notifying espresso machine of cup at slot {slotIndex}");
                parentMachine.OnCupDropped(slotIndex, item);
            }
        }
        else
        {
            LogDebug("WARNING: parentMachine reference is null, cannot notify");
        }
    }
    
    private void OnTransformChildrenChanged()
    {
        // Check if a child was removed
        if (transform.childCount == 0 && parentMachine != null)
        {
            if (isPortafilterZone)
            {
                LogDebug($"Notifying espresso machine of portafilter removal at slot {slotIndex}");
                parentMachine.OnPortafilterRemoved(slotIndex);
            }
            else
            {
                LogDebug($"Notifying espresso machine of cup removal at slot {slotIndex}");
                parentMachine.OnCupRemoved(slotIndex);
            }
        }
    }
    
    private void LogDebug(string message)
    {
        if (debugLogs)
        {
            Debug.Log($"[EspressoMachineDropZone] {message}");
        }
    }
}