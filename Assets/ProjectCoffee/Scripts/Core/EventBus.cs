using System;
using ProjectCoffee.Services;
using UnityEngine;

namespace ProjectCoffee.Core
{
    /// <summary>
    /// Central event hub for game-wide communication
    /// </summary>
    public static class EventBus
    {
        // System events
        public static event Action OnServicesInitialized;
        
        // Game state events
        public static event Action<int> OnMoneyChanged;
        public static event Action<int> OnDayStarted;
        public static event Action<int> OnDayEnded;
        
        // Machine events
        public static event Action<string, MachineState> OnMachineStateChanged;
        public static event Action<string, int> OnMachineUpgraded;
        public static event Action<string, MachineConfig, int> OnMachineRegistered;
        
        // Customer/order events
        public static event Action<string> OnCustomerArrived;
        public static event Action<string, bool> OnOrderCompleted;
        
        // Item interactions
        public static event Action<string, string> OnItemsInteracted;
        public static event Action<string> OnItemCreated;
        public static event Action<string> OnItemConsumed;
        
        /// <summary>
        /// Notify that all services have been initialized
        /// </summary>
        public static void NotifyServicesInitialized()
        {
            OnServicesInitialized?.Invoke();
        }
        
        /// <summary>
        /// Notify money value changed
        /// </summary>
        public static void NotifyMoneyChanged(int newAmount)
        {
            OnMoneyChanged?.Invoke(newAmount);
        }
        
        /// <summary>
        /// Notify day started
        /// </summary>
        public static void NotifyDayStarted(int dayNumber)
        {
            OnDayStarted?.Invoke(dayNumber);
        }
        
        /// <summary>
        /// Notify day ended
        /// </summary>
        public static void NotifyDayEnded(int dayNumber)
        {
            OnDayEnded?.Invoke(dayNumber);
        }
        
        /// <summary>
        /// Notify machine state changed
        /// </summary>
        public static void NotifyMachineStateChanged(string machineId, MachineState newState)
        {
            OnMachineStateChanged?.Invoke(machineId, newState);
        }
        
        /// <summary>
        /// Notify machine upgraded
        /// </summary>
        public static void NotifyMachineUpgraded(string machineId, int newLevel)
        {
        Debug.Log($"EventBus.NotifyMachineUpgraded: {machineId} to level {newLevel}");
            
        if (OnMachineUpgraded == null)
        {
            Debug.LogWarning("EventBus.NotifyMachineUpgraded: No subscribers to OnMachineUpgraded event!");
        }
        
        OnMachineUpgraded?.Invoke(machineId, newLevel);
    }
        
        /// <summary>
        /// Notify machine registered for upgrades
        /// </summary>
        public static void NotifyMachineRegistered(string machineId, MachineConfig config, int currentLevel = 0)
        {
        Debug.Log($"EventBus.NotifyMachineRegistered: {machineId} ({config.displayName}) with level {currentLevel}");
            
        if (OnMachineRegistered == null)
        {
            Debug.LogWarning("EventBus.NotifyMachineRegistered: No subscribers to OnMachineRegistered event! Make sure UpgradeService is initialized first.");
        }
        
        OnMachineRegistered?.Invoke(machineId, config, currentLevel);
    }
        
        /// <summary>
        /// Notify customer arrived with an order
        /// </summary>
        public static void NotifyCustomerArrived(string customerId)
        {
            OnCustomerArrived?.Invoke(customerId);
        }
        
        /// <summary>
        /// Notify order completed (success or failure)
        /// </summary>
        public static void NotifyOrderCompleted(string orderId, bool success)
        {
            OnOrderCompleted?.Invoke(orderId, success);
        }
        
        /// <summary>
        /// Notify items interacted (e.g., combined)
        /// </summary>
        public static void NotifyItemsInteracted(string sourceItemId, string targetItemId)
        {
            OnItemsInteracted?.Invoke(sourceItemId, targetItemId);
        }
        
        /// <summary>
        /// Notify item created
        /// </summary>
        public static void NotifyItemCreated(string itemId)
        {
            OnItemCreated?.Invoke(itemId);
        }
        
        /// <summary>
        /// Notify item consumed
        /// </summary>
        public static void NotifyItemConsumed(string itemId)
        {
            OnItemConsumed?.Invoke(itemId);
        }
    }
}