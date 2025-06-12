using UnityEngine;
using ProjectCoffee.Interaction.Helpers;

namespace ProjectCoffee.Machines.Components
{
    /// <summary>
    /// Base class for espresso machine drop zones with proper item tracking
    /// </summary>
    public abstract class EspressoDropZoneBase : DropZone
    {
        [SerializeField] protected int slotIndex;
        // Use the fully qualified name to avoid namespace conflict
        protected ProjectCoffee.Machines.EspressoMachine.EspressoMachine espressoMachine;
        protected DropZoneItemTracker itemTracker;
        
        protected virtual void Start()
        {
            // base.Awake();
            
            // Find the espresso machine in parent - use fully qualified name
            espressoMachine = GetComponentInParent<ProjectCoffee.Machines.EspressoMachine.EspressoMachine>();
            if (espressoMachine == null)
            {
                Debug.LogError($"EspressoDropZoneBase on {gameObject.name} requires an EspressoMachine in parent!");
            }
            
            // Add item tracker if not present
            itemTracker = GetComponent<DropZoneItemTracker>();
            if (itemTracker == null)
            {
                itemTracker = gameObject.AddComponent<DropZoneItemTracker>();
            }
            
            // Setup accept predicate
            SetupAcceptPredicate();
        }
        
        protected abstract void SetupAcceptPredicate();
        
        public override bool CanAccept(Draggable item)
        {
            // First check with tracker
            if (!itemTracker.CanAcceptItem(item))
            {
                LogDebug($"Item tracker rejected {item.name}");
                return false;
            }
            
            // Then check base conditions
            return base.CanAccept(item);
        }
        
        public override void OnItemDropped(Draggable item)
        {
            // Update tracker first
            itemTracker.SetItem(item);
            
            // Ensure item has state manager BEFORE any state changes occur
            var stateManager = item.GetComponent<DraggableStateManager>();
            if (stateManager == null)
            {
                // CRITICAL: Add state manager while item is still in normal state
                stateManager = item.gameObject.AddComponent<DraggableStateManager>();
                
                // Force it to store the correct original state (draggable enabled)
                // This prevents issues when auto-brewing starts immediately
                if (stateManager != null)
                {
                    stateManager.ForceStoreOriginalState(true);
                }
            }
            
            // Call base implementation
            base.OnItemDropped(item);
        }
        
        public override void OnItemRemoved(Draggable item)
        {
            // Clear tracker
            itemTracker.ClearItem();
            
            // Ensure state is restored
            var stateManager = item.GetComponent<DraggableStateManager>();
            if (stateManager != null)
            {
                stateManager.ForceReset();
            }
            
            // Call base implementation
            base.OnItemRemoved(item);
        }
        
        /// <summary>
        /// Set items in this zone to processing state
        /// </summary>
        public void SetProcessingState(bool isProcessing)
        {
            // DISABLED: Don't change visual state to prevent transparency issues
            // The EspressoMachine will handle dragging state directly
            
            /*
            if (itemTracker.HasItem)
            {
                var stateManager = itemTracker.CurrentItem.GetComponent<DraggableStateManager>();
                if (stateManager != null)
                {
                    // Only change state if it's different from current state
                    bool currentlyProcessing = !itemTracker.CurrentItem.GetComponent<Draggable>().enabled;
                    if (currentlyProcessing != isProcessing)
                    {
                        stateManager.SetProcessingState(isProcessing);
                        Debug.Log($"[EspressoDropZoneBase] Set processing state for {itemTracker.CurrentItem.name} to {isProcessing}");
                    }
                }
            }
            */
            
            Debug.Log($"[EspressoDropZoneBase] Processing state change ignored to prevent visual issues");
        }
    }
}
