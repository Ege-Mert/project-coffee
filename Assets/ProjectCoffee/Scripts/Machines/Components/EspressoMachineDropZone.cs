using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectCoffee.Interaction.Helpers;
using ProjectCoffee.Machines.Components;
using ProjectCoffee.Services.Interfaces;

/// <summary>
/// Helper class for espresso machine drop zones with proper item tracking
/// </summary>
public class EspressoMachineDropZone : EspressoDropZoneBase
{
    [SerializeField] private bool isPortafilterZone; // True for portafilter, false for cup
    
    // Add this property to expose the slotIndex field
    public int SlotIndex 
    { 
        get => slotIndex; 
        set => slotIndex = value; 
    }
    
    protected override void SetupAcceptPredicate()
    {
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
        
        LogDebug($"DropZone initialized: isPortafilterZone={isPortafilterZone}, slotIndex={slotIndex}");
    }
    
    protected override void RaiseItemDropped(Draggable item)
    {
        if (espressoMachine != null)
        {
            if (isPortafilterZone && item is Portafilter)
            {
                LogDebug($"Notifying espresso machine of portafilter at slot {slotIndex}");
                espressoMachine.OnPortafilterDropped(slotIndex, item);
            }
            else if (!isPortafilterZone && item is Cup)
            {
                LogDebug($"Notifying espresso machine of cup at slot {slotIndex}");
                espressoMachine.OnCupDropped(slotIndex, item);
            }
        }
    }
    
    protected override void RaiseItemRemoved(Draggable item)
    {
        if (espressoMachine != null)
        {
            if (isPortafilterZone)
            {
                LogDebug($"Notifying espresso machine of portafilter removal at slot {slotIndex}");
                espressoMachine.OnPortafilterRemoved(slotIndex);
            }
            else
            {
                LogDebug($"Notifying espresso machine of cup removal at slot {slotIndex}");
                espressoMachine.OnCupRemoved(slotIndex);
            }
        }
    }
}
