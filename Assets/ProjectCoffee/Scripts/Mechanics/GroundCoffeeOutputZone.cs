using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
/// <summary>
/// Extended drop zone specifically for ground coffee output
/// </summary>
public class GroundCoffeeOutputZone : DropZoneUI
{
    [SerializeField] private CoffeeGrinderUI parentGrinder;
    
    // Allow setting the parent grinder from code
    public void SetParentGrinder(CoffeeGrinderUI grinder)
    {
        if (parentGrinder == null)
        {
            parentGrinder = grinder;
            print($"Set parent grinder on {gameObject.name}");
        }
    }
    
    public override bool CanAccept(DraggableUI item)
    {
        // IMPORTANT CHANGE: Allow accepting ground coffee items
        if (item is GroundCoffeeUI)
        {
            return true;
        }
        
        // This zone is only for ground coffee
        return false;
    }
    
    private void OnTransformChildrenChanged()
    {
        // Check if a child was removed
        print($"Children changed. Current count: {transform.childCount}");
        if (transform.childCount == 0 && parentGrinder != null)
        {
            print("All children removed - notifying grinder");
            parentGrinder.OnGroundCoffeeRemoved();
        }
    }
    
    public override void OnItemDropped(DraggableUI item)
    {
        print($"Item dropped on output zone: {item.name}");
        
        // Call base implementation for positioning
        base.OnItemDropped(item);
        
        // Handle special case of ground coffee
        if (item is GroundCoffeeUI && parentGrinder != null)
        {
            // If we need any special handling when coffee is placed here
        }
    }
    
    public override void OnItemRemoved(DraggableUI item)
    {
        print($"Item removed from ground coffee output zone: {item.name}");
        
        base.OnItemRemoved(item);
        
        if (item is GroundCoffeeUI && parentGrinder != null)
        {
            print("Notifying parent grinder that ground coffee was removed");
            parentGrinder.OnGroundCoffeeRemoved();
        }
    }
}