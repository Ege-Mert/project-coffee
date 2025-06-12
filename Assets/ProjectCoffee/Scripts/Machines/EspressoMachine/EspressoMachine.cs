using UnityEngine;
using ProjectCoffee.Services;
using ProjectCoffee.Core;
using ProjectCoffee.Services.Interfaces;
using ProjectCoffee.Machines;
using ProjectCoffee.Machines.Components;
using ProjectCoffee.Machines.EspressoMachine.Logic;
using ProjectCoffee.Items;
using ProjectCoffee.Interaction;
using ProjectCoffee.Interaction.Helpers;
using System.Collections;
using System.Collections.Generic;
using CoreServices = ProjectCoffee.Core.Services;

namespace ProjectCoffee.Machines.EspressoMachine
{
    /// <summary>
    /// Espresso machine with DropZone-based slot tracking and upgrade-based slot visibility.
    /// Level 0: 2 slots, 5s brew time, manual brewing
    /// Level 1: 2 slots, 2.5s brew time (half), manual brewing  
    /// Level 2: 4 slots, 2.5s brew time, automatic brewing
    /// Slot visibility is managed by EspressoMachineUI.
    /// </summary>
    public class EspressoMachine : ProjectCoffee.Machines.MachineBase
    {
        [Header("Espresso Machine Drop Zones")]
        [SerializeField] private List<EspressoMachineDropZone> portafilterZones = new List<EspressoMachineDropZone>();
        [SerializeField] private List<EspressoMachineDropZone> cupZones = new List<EspressoMachineDropZone>();
        
        private EspressoMachineService service;
        private Dictionary<int, Portafilter> currentPortafilters = new Dictionary<int, Portafilter>();
        private Dictionary<int, Cup> currentCups = new Dictionary<int, Cup>();
        
        #region Properties
        
        public bool CanBrewAnySlot() => service?.CanBrewAnySlot() ?? false;
        public int GetSlotCount() => GetAvailableSlotCount();
        public EspressoMachineService GetService() => service;
        
        #endregion
        
        #region Machine Lifecycle
        
        protected override void InitializeMachine()
        {
            InitializeService();
            SetupDropZones();
            SubscribeToEvents();
            
            Debug.Log($"EspressoMachine: Initialized with {GetAvailableSlotCount()} active slots");
        }
        
        private void InitializeService()
        {
            service = new EspressoMachineService(config as EspressoMachineConfig);
            ServiceManager.Instance?.RegisterMachineService<IEspressoMachineService>(service);
        }
        
        private void SetupDropZones()
        {
            // Configure portafilter zones
            for (int i = 0; i < portafilterZones.Count; i++)
            {
                if (portafilterZones[i] != null)
                {
                    portafilterZones[i].SlotIndex = i;
                    
                    // Add item tracker if not present
                    var tracker = portafilterZones[i].GetComponent<DropZoneItemTracker>();
                    if (tracker == null)
                        tracker = portafilterZones[i].gameObject.AddComponent<DropZoneItemTracker>();
                }
            }
            
            // Configure cup zones
            for (int i = 0; i < cupZones.Count; i++)
            {
                if (cupZones[i] != null)
                {
                    cupZones[i].SlotIndex = i;
                    
                    // Add item tracker if not present
                    var tracker = cupZones[i].GetComponent<DropZoneItemTracker>();
                    if (tracker == null)
                        tracker = cupZones[i].gameObject.AddComponent<DropZoneItemTracker>();
                }
            }
        }
        
        private void SubscribeToEvents()
        {
            if (service != null)
            {
                service.OnStateChanged += TransitionToState;
                service.OnProgressChanged += UpdateProgress;
                service.OnProcessCompleted += CompleteProcess;
                service.OnBrewingCompleted += HandleBrewingCompleted;
            }
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromEvents();
        }
        
        private void UnsubscribeFromEvents()
        {
            if (service != null)
            {
                service.OnStateChanged -= TransitionToState;
                service.OnProgressChanged -= UpdateProgress;
                service.OnProcessCompleted -= CompleteProcess;
                service.OnBrewingCompleted -= HandleBrewingCompleted;
            }
        }
        
        #endregion
        
        #region Slot Management
        
        private int GetAvailableSlotCount()
        {
            return upgradeLevel >= 2 ? 4 : 2;
        }
        
        #endregion
        
        #region Unity Updates
        
        private void Update()
        {
            CheckSlotsPresence();
            
            // Update brewing progress through service
            var espressoService = CoreServices.Get<IEspressoMachineService>();
            espressoService?.UpdateBrewing(Time.deltaTime);
        }
        
        private void CheckSlotsPresence()
        {
            int availableSlots = GetAvailableSlotCount();
            
            for (int i = 0; i < availableSlots; i++)
            {
                CheckPortafilterInSlot(i);
                CheckCupInSlot(i);
            }
        }
        
        private void CheckPortafilterInSlot(int slotIndex)
        {
            if (slotIndex >= portafilterZones.Count || portafilterZones[slotIndex] == null) 
                return;
            
            var tracker = portafilterZones[slotIndex].GetComponent<DropZoneItemTracker>();
            if (tracker == null) return;
            
            // Check for portafilter added
            if (tracker.HasItem && !currentPortafilters.ContainsKey(slotIndex))
            {
                Portafilter portafilter = tracker.CurrentItem as Portafilter;
                if (portafilter != null)
                {
                    currentPortafilters[slotIndex] = portafilter;
                    
                    bool hasGroundCoffee = portafilter.HasGroundCoffee;
                    float coffeeQuality = portafilter.GetCoffeeQualityFactor();
                    
                    service?.SetPortafilter(slotIndex, true, hasGroundCoffee, coffeeQuality);
                    
                    Debug.Log($"EspressoMachine: Portafilter added to slot {slotIndex} - HasCoffee: {hasGroundCoffee}, Quality: {coffeeQuality:F2}");
                }
            }
            // Check for portafilter removed
            else if (!tracker.HasItem && currentPortafilters.ContainsKey(slotIndex))
            {
                currentPortafilters.Remove(slotIndex);
                service?.SetPortafilter(slotIndex, false);
                Debug.Log($"EspressoMachine: Portafilter removed from slot {slotIndex}");
            }
            // Check for portafilter content changes
            else if (currentPortafilters.ContainsKey(slotIndex))
            {
                var portafilter = currentPortafilters[slotIndex];
                bool currentHasCoffee = portafilter.HasGroundCoffee;
                float currentQuality = portafilter.GetCoffeeQualityFactor();
                
                // Update service with current state
                service?.SetPortafilter(slotIndex, true, currentHasCoffee, currentQuality);
            }
        }
        
        private void CheckCupInSlot(int slotIndex)
        {
            if (slotIndex >= cupZones.Count || cupZones[slotIndex] == null) 
                return;
            
            var tracker = cupZones[slotIndex].GetComponent<DropZoneItemTracker>();
            if (tracker == null) return;
            
            // Check for cup added
            if (tracker.HasItem && !currentCups.ContainsKey(slotIndex))
            {
                Cup cup = tracker.CurrentItem as Cup;
                if (cup != null)
                {
                    currentCups[slotIndex] = cup;
                    service?.SetCup(slotIndex, true);
                    Debug.Log($"EspressoMachine: Cup added to slot {slotIndex}");
                }
            }
            // Check for cup removed
            else if (!tracker.HasItem && currentCups.ContainsKey(slotIndex))
            {
                currentCups.Remove(slotIndex);
                service?.SetCup(slotIndex, false);
                Debug.Log($"EspressoMachine: Cup removed from slot {slotIndex}");
            }
        }
        
        #endregion
        
        #region Brewing Control
        
        public void OnBrewButtonClicked()
        {
            var espressoService = CoreServices.Get<IEspressoMachineService>();
            espressoService?.BrewAllReadySlots();
            Debug.Log("EspressoMachine: Brew button clicked - starting all ready slots");
        }
        
        public override void StartProcess()
        {
            base.StartProcess();
            OnBrewButtonClicked();
        }
        
        #endregion
        
        #region Brewing Completion
        
        private void HandleBrewingCompleted(int slotIndex)
        {
            if (!currentCups.ContainsKey(slotIndex) || !currentPortafilters.ContainsKey(slotIndex)) 
                return;
            
            var cup = currentCups[slotIndex];
            var portafilter = currentPortafilters[slotIndex];
            
            var espressoService = CoreServices.Get<IEspressoMachineService>();
            var slot = espressoService?.GetSlot(slotIndex);
            
            if (cup != null && portafilter != null && slot != null)
            {
                // Calculate espresso amount based on quality
                float qualityFactor = slot.coffeeQuality;
                float shotAmount = 2f;
                float adjustedAmount = shotAmount * Mathf.Lerp(0.7f, 1.2f, qualityFactor);
                
                // Add espresso to cup
                cup.TryAddItem("espresso", adjustedAmount);
                
                // Clear portafilter
                portafilter.Clear();
                
                // Show quality notification
                string qualityDescription = GetQualityDescription(qualityFactor);
                NotifyUser($"{qualityDescription} espresso ready!");
                
                Debug.Log($"EspressoMachine: Brewing completed for slot {slotIndex} - {qualityDescription} quality ({adjustedAmount:F1}ml)");
            }
            
            // Restore item interaction after a brief delay
            StartCoroutine(RestoreSlotItemsToNormalState(slotIndex));
        }
        
        private IEnumerator RestoreSlotItemsToNormalState(int slotIndex)
        {
            yield return new WaitForSeconds(0.1f);
            
            if (currentPortafilters.ContainsKey(slotIndex))
            {
                var portafilter = currentPortafilters[slotIndex];
                RestoreItemState(portafilter.gameObject);
            }
            
            if (currentCups.ContainsKey(slotIndex))
            {
                var cup = currentCups[slotIndex];
                RestoreItemState(cup.gameObject);
            }
        }
        
        private void RestoreItemState(GameObject item)
        {
            var draggable = item.GetComponent<Draggable>();
            if (draggable != null)
            {
                draggable.enabled = true;
                draggable.RefreshCanvasReference();
            }
            
            var canvasGroup = item.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
                canvasGroup.alpha = 1f;
            }
        }
        
        private string GetQualityDescription(float qualityFactor)
        {
            return qualityFactor switch
            {
                >= 0.9f => "Perfect",
                >= 0.7f => "Excellent",
                >= 0.5f => "Good",
                >= 0.3f => "Okay",
                _ => "Poor"
            };
        }
        
        #endregion
        
        #region Legacy Interface Support
        
        // These methods maintain backward compatibility with existing UI and components
        
        public void OnPortafilterDropped(int slotIndex, Draggable item)
        {
            if (item is Portafilter portafilter)
            {
                currentPortafilters[slotIndex] = portafilter;
                
                bool hasGroundCoffee = portafilter.HasGroundCoffee;
                float coffeeQuality = portafilter.GetCoffeeQualityFactor();
                
                service?.SetPortafilter(slotIndex, true, hasGroundCoffee, coffeeQuality);
                NotifyUser("Portafilter placed in espresso machine");
            }
        }
        
        public void OnPortafilterRemoved(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < portafilterZones.Count)
            {
                currentPortafilters.Remove(slotIndex);
                service?.SetPortafilter(slotIndex, false);
            }
        }
        
        public void OnCupDropped(int slotIndex, Draggable item)
        {
            if (item is Cup cup)
            {
                currentCups[slotIndex] = cup;
                service?.SetCup(slotIndex, true);
                NotifyUser("Cup placed in espresso machine");
            }
        }
        
        public void OnCupRemoved(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < cupZones.Count)
            {
                currentCups.Remove(slotIndex);
                service?.SetCup(slotIndex, false);
            }
        }
        
        public Dictionary<int, Portafilter> GetCurrentPortafilters() => currentPortafilters;
        public Dictionary<int, Cup> GetCurrentCups() => currentCups;
        
        #endregion
        
        #region MachineBase Overrides
        
        protected override void ApplyUpgrade(int previousLevel, int newLevel)
        {
            service?.SetUpgradeLevel(newLevel);
            
            string message = newLevel switch
            {
                1 => "Faster brewing time - brews twice as fast!",
                2 => "Extra brewing slots and auto-brewing activated!",
                _ => $"Espresso machine upgraded to level {newLevel}!"
            };
            
            NotifyUser(message);
            Debug.Log($"EspressoMachine: {message} (Available slots: {GetAvailableSlotCount()})");
        }
        
        #endregion
        
        #region Debug Support
        
        [ContextMenu("Debug Espresso Machine State")]
        private void DebugMachineState()
        {
            if (service != null)
            {
                Debug.Log(service.GetDebugInfo());
            }
            
            Debug.Log($"EspressoMachine: Upgrade Level {upgradeLevel}, Available Slots: {GetAvailableSlotCount()}");
            Debug.Log($"EspressoMachine: Current Portafilters: {currentPortafilters.Count}, Current Cups: {currentCups.Count}");
            
            for (int i = 0; i < GetAvailableSlotCount(); i++)
            {
                bool hasPortafilter = currentPortafilters.ContainsKey(i);
                bool hasCup = currentCups.ContainsKey(i);
                
                Debug.Log($"Slot {i}: Portafilter={hasPortafilter}, Cup={hasCup}");
            }
        }
        
        [ContextMenu("Force Test Upgrade Level 1")]
        private void ForceTestUpgradeLevel1()
        {
            Debug.Log("EspressoMachine: Forcing upgrade to level 1 for testing");
            
            string machineId = Config?.machineId ?? gameObject.name;
            EventBus.NotifyMachineUpgraded(machineId, 1);
        }
        
        [ContextMenu("Force Test Upgrade Level 2")]
        private void ForceTestUpgradeLevel2()
        {
            Debug.Log("EspressoMachine: Forcing upgrade to level 2 for testing");
            
            // Simulate what EventBus would do - trigger the upgrade through the normal system
            string machineId = Config?.machineId ?? gameObject.name;
            
            // Use EventBus to trigger the upgrade (this will call HandleMachineUpgraded)
            EventBus.NotifyMachineUpgraded(machineId, 2);
        }
        
        #endregion
    }
}
