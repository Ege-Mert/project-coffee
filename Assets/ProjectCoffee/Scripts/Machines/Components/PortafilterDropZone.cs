using UnityEngine;

/// <summary>
/// Advanced drop zone specifically for portafilters
/// </summary>
public class PortafilterDropZone : DropZone
{
    [SerializeField] private CoffeeGrammingMachine parentMachine;
    
    public override bool CanAccept(Draggable item)
    {
        if (!base.CanAccept(item))
            return false;
            
        return item is Portafilter;
    }
    
    public override void OnItemDropped(Draggable item)
    {
        base.OnItemDropped(item);
        
        if (parentMachine != null && item is Portafilter)
        {
            parentMachine.OnPortafilterDropped(item);
        }
    }
    
    private void OnTransformChildrenChanged()
    {
        // Check if child was removed
        if (transform.childCount == 0 && parentMachine != null)
        {
            // Find the removed item - no longer a child, so we need to use a different approach
            // This is a simplification, in a real implementation you might want to cache the reference
            Portafilter[] portafilters = FindObjectsOfType<Portafilter>();
            foreach (Portafilter portafilter in portafilters)
            {
                if (portafilter.transform.parent != transform && 
                    portafilter.gameObject.activeSelf && 
                    Vector3.Distance(portafilter.transform.position, transform.position) < 500f) // Arbitrary distance
                {
                    parentMachine.OnPortafilterRemoved(portafilter);
                    break;
                }
            }
        }
    }
}