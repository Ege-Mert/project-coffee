using UnityEngine;

/// <summary>
/// Advanced drop zone specifically for ground coffee
/// </summary>
public class GroundCoffeeDropZone : DropZoneUI
{
    [SerializeField] private CoffeeGrammingMachineUI parentMachine;
    
    public override bool CanAccept(DraggableUI item)
    {
        if (!base.CanAccept(item))
            return false;
            
        return item is GroundCoffeeUI && parentMachine != null && 
               parentMachine.transform.childCount > 0; // Has a portafilter
    }
    
    public override void OnItemDropped(DraggableUI item)
    {
        base.OnItemDropped(item);
        
        if (parentMachine != null && item is GroundCoffeeUI)
        {
            parentMachine.OnGroundCoffeeDropped(item);
        }
    }
}