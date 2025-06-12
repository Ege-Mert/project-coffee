using UnityEngine;
using ProjectCoffee.Interaction;
using System;

namespace ProjectCoffee.Machines.EspressoMachine.Components
{
    /// <summary>
    /// Clean component for tracking slot contents in espresso machine.
    /// Focused on Unity integration with simple presence detection.
    /// </summary>
    public class EspressoSlotTracker : MonoBehaviour
    {
        #region Events
        
        public event Action<int, Portafilter> OnPortafilterAdded;
        public event Action<int, Portafilter> OnPortafilterRemoved;
        public event Action<int, Cup> OnCupAdded;
        public event Action<int, Cup> OnCupRemoved;
        
        #endregion
        
        #region Serialized Fields
        
        [Header("Slot Configuration")]
        [SerializeField] private int slotIndex = 0;
        [SerializeField] private Transform portafilterZone;
        [SerializeField] private Transform cupZone;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        
        #endregion
        
        #region Private Fields
        
        private Portafilter currentPortafilter;
        private Cup currentCup;
        
        #endregion
        
        #region Properties
        
        public int SlotIndex 
        { 
            get => slotIndex; 
            set => slotIndex = value; 
        }
        
        public bool HasPortafilter => currentPortafilter != null;
        public bool HasCup => currentCup != null;
        public Portafilter CurrentPortafilter => currentPortafilter;
        public Cup CurrentCup => currentCup;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            ValidateConfiguration();
        }
        
        private void Update()
        {
            CheckPortafilterPresence();
            CheckCupPresence();
        }
        
        #endregion
        
        #region Configuration Validation
        
        private void ValidateConfiguration()
        {
            if (portafilterZone == null)
            {
                Debug.LogError($"EspressoSlotTracker: Portafilter zone not assigned for slot {slotIndex}!");
            }
            
            if (cupZone == null)
            {
                Debug.LogError($"EspressoSlotTracker: Cup zone not assigned for slot {slotIndex}!");
            }
        }
        
        #endregion
        
        #region Presence Detection
        
        private void CheckPortafilterPresence()
        {
            CheckItemPresence<Portafilter>(
                portafilterZone, 
                ref currentPortafilter,
                OnPortafilterAdded,
                OnPortafilterRemoved,
                "Portafilter"
            );
        }
        
        private void CheckCupPresence()
        {
            CheckItemPresence<Cup>(
                cupZone,
                ref currentCup,
                OnCupAdded,
                OnCupRemoved,
                "Cup"
            );
        }
        
        /// <summary>
        /// Generic method for checking item presence in a zone
        /// </summary>
        private void CheckItemPresence<T>(
            Transform zone, 
            ref T currentItem, 
            Action<int, T> onAdded, 
            Action<int, T> onRemoved,
            string itemName) where T : Component
        {
            if (zone == null) return;
            
            T foundItem = null;
            
            // Check if item is present in zone
            if (zone.childCount > 0)
            {
                foundItem = zone.GetComponentInChildren<T>();
            }
            
            // Handle item added
            if (foundItem != null && currentItem == null)
            {
                currentItem = foundItem;
                onAdded?.Invoke(slotIndex, foundItem);
                
                if (enableDebugLogs)
                    Debug.Log($"EspressoSlotTracker: {itemName} added to slot {slotIndex}");
            }
            // Handle item removed
            else if (foundItem == null && currentItem != null)
            {
                var removedItem = currentItem;
                currentItem = null;
                onRemoved?.Invoke(slotIndex, removedItem);
                
                if (enableDebugLogs)
                    Debug.Log($"EspressoSlotTracker: {itemName} removed from slot {slotIndex}");
            }
            // Handle item changed (different instance)
            else if (foundItem != null && currentItem != null && foundItem != currentItem)
            {
                var oldItem = currentItem;
                currentItem = foundItem;
                
                // Fire both events for clean state management
                onRemoved?.Invoke(slotIndex, oldItem);
                onAdded?.Invoke(slotIndex, foundItem);
                
                if (enableDebugLogs)
                    Debug.Log($"EspressoSlotTracker: {itemName} changed in slot {slotIndex}");
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Manually sets the portafilter zone reference
        /// </summary>
        public void SetPortafilterZone(Transform zone)
        {
            portafilterZone = zone;
        }
        
        /// <summary>
        /// Manually sets the cup zone reference
        /// </summary>
        public void SetCupZone(Transform zone)
        {
            cupZone = zone;
        }
        
        /// <summary>
        /// Gets current slot state for debugging
        /// </summary>
        public string GetSlotState()
        {
            return $"Slot {slotIndex}: Portafilter={HasPortafilter}, Cup={HasCup}";
        }
        
        /// <summary>
        /// Forces a check of all item presence (useful after scene changes)
        /// </summary>
        public void ForceCheck()
        {
            CheckPortafilterPresence();
            CheckCupPresence();
        }
        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmosSelected()
        {
            if (portafilterZone != null)
            {
                Gizmos.color = HasPortafilter ? Color.green : Color.yellow;
                Gizmos.DrawWireCube(portafilterZone.position, Vector3.one * 0.5f);
            }
            
            if (cupZone != null)
            {
                Gizmos.color = HasCup ? Color.blue : Color.cyan;
                Gizmos.DrawWireCube(cupZone.position, Vector3.one * 0.3f);
            }
        }
        
        #endregion
    }
}
