using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Helper class for espresso machine drop zones
/// </summary>
public class EspressoMachineDropZone : DropZoneUI
{
    [SerializeField] private EspressoMachineUI parentMachine;
    [SerializeField] private int slotIndex;
    [SerializeField] private bool isPortafilterZone; // True for portafilter, false for cup
    
    public override bool CanAccept(DraggableUI item)
    {
        if (!base.CanAccept(item))
            return false;
            
        if (isPortafilterZone)
        {
            return item is Portafilter;
        }
        else
        {
            return item is Cup;
        }
    }
    
    public override void OnItemDropped(DraggableUI item)
    {
        base.OnItemDropped(item);
        
        if (parentMachine != null)
        {
            if (isPortafilterZone && item is Portafilter)
            {
                parentMachine.OnPortafilterDropped(slotIndex, item);
            }
            else if (!isPortafilterZone && item is Cup)
            {
                parentMachine.OnCupDropped(slotIndex, item);
            }
        }
    }
    
    private void OnTransformChildrenChanged()
    {
        // Check if a child was removed
        if (transform.childCount == 0 && parentMachine != null)
        {
            if (isPortafilterZone)
            {
                parentMachine.OnPortafilterRemoved(slotIndex);
            }
            else
            {
                parentMachine.OnCupRemoved(slotIndex);
            }
        }
    }
}