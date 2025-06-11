using UnityEngine;
using ProjectCoffee.Machines;
using ProjectCoffee.Machines.Dosing;

/// <summary>
/// Advanced drop zone specifically for ground coffee
/// </summary>
public class GroundCoffeeDropZone : DropZone
{
    [SerializeField] private DosingMachine parentMachine;
    
    public override bool CanAccept(Draggable item)
    {
        if (!base.CanAccept(item))
            return false;
            
        // Accept ground coffee regardless of portafilter presence
        return item is GroundCoffee && parentMachine != null;
    }
    
    public override void OnItemDropped(Draggable item)
    {
        base.OnItemDropped(item);
        
        if (parentMachine != null && item is GroundCoffee)
        {
            parentMachine.OnGroundCoffeeDropped(item);
        }
    }
}