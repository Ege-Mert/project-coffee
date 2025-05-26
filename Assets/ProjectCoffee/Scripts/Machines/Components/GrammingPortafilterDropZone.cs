using System.Collections;
using System.Collections.Generic;
using ProjectCoffee.Interaction.Helpers;
using UnityEngine;


namespace ProjectCoffee.Machines.Components
{
    /// <summary>
    /// Updated drop zone specifically for portafilters in the gramming machine
    /// </summary>
    public class GrammingPortafilterDropZone : DropZone
    {
        [SerializeField] private CoffeeGrammingMachine parentMachine;
        
        private Portafilter currentPortafilter;
        private DropZoneItemTracker itemTracker;
        
        protected virtual void Start()
        {
            // Add item tracker if not present
            itemTracker = GetComponent<DropZoneItemTracker>();
            if (itemTracker == null)
            {
                itemTracker = gameObject.AddComponent<DropZoneItemTracker>();
            }
            
            // Set the accept predicate
            AcceptPredicate = (item) => item is Portafilter;
            LogDebug("GrammingPortafilterDropZone initialized with item tracker");
        }
        
        public override bool CanAccept(Draggable item)
        {
            // First check with tracker
            if (!itemTracker.CanAcceptItem(item))
            {
                LogDebug($"Item tracker rejected {item.name}");
                return false;
            }
            
            // If it's the same item already tracked, always allow it (for OnEndDrag validation)
            if (itemTracker.CurrentItem == item)
            {
                LogDebug($"Allowing same tracked item {item.name}");
                return true;
            }
            
            // For new items, check base conditions
            return base.CanAccept(item);
        }
        
        public override void OnItemDropped(Draggable item)
        {
            // Update tracker first
            itemTracker.SetItem(item);
            
            // Ensure item has state manager
            var stateManager = item.GetComponent<DraggableStateManager>();
            if (stateManager == null)
            {
                stateManager = item.gameObject.AddComponent<DraggableStateManager>();
            }
            
            // Call base implementation
            base.OnItemDropped(item);
            
            // Track the current portafilter
            currentPortafilter = item as Portafilter;
            
            // Notify the machine
            if (parentMachine != null)
            {
                LogDebug($"Notifying machine that portafilter was placed");
                parentMachine.OnPortafilterDropped(item);
            }
        }
        
        public override void OnItemRemoved(Draggable item)
        {
            // Clear tracker
            itemTracker.ClearItem();
            
            // Call base
            base.OnItemRemoved(item);
            
            // Notify the machine
            if (parentMachine != null && currentPortafilter != null)
            {
                LogDebug($"Notifying machine that portafilter was removed");
                parentMachine.OnPortafilterRemoved(currentPortafilter);
            }
            
            // Clear our reference
            currentPortafilter = null;
        }
        
        // Override OnTransformChildrenChanged to use OnItemRemoved
        private void OnTransformChildrenChanged()
        {
            if (transform.childCount == 0 && currentPortafilter != null)
            {
                OnItemRemoved(currentPortafilter);
            }
        }
    }
}

