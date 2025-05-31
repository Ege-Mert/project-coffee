using UnityEngine;
using ProjectCoffee.Services;
using ProjectCoffee.Core;
using ProjectCoffee.Services.Interfaces;
using ProjectCoffee.Machines;
using ProjectCoffee.Interaction.Helpers;
using System.Collections;
using System.Collections.Generic;
using CoreServices = ProjectCoffee.Core.Services;

namespace ProjectCoffee.Machines
{
    public class EspressoMachine : MachineBase
    {
        [Header("Espresso Machine Components")]
        [SerializeField] private List<EspressoMachineDropZone> portafilterZones = new List<EspressoMachineDropZone>();
        [SerializeField] private List<EspressoMachineDropZone> cupZones = new List<EspressoMachineDropZone>();
        
        private EspressoMachineService service;
        private Dictionary<int, Portafilter> currentPortafilters = new Dictionary<int, Portafilter>();
        private Dictionary<int, Cup> currentCups = new Dictionary<int, Cup>();
        private Dictionary<int, float> lastStateUpdateTime = new Dictionary<int, float>();
        private const float STATE_UPDATE_COOLDOWN = 0.1f;
        
        protected override void InitializeMachine()
        {
            service = new EspressoMachineService(config as EspressoMachineConfig);
            ServiceManager.Instance?.RegisterMachineService<IEspressoMachineService>(service);
            
            SetupServiceEvents();
            InitializeDropZoneTrackers();
            ConfigureDropZones();
            EnsureStateManagersOnAllItems();
        }
        
        private void SetupServiceEvents()
        {
            if (service != null)
            {
                service.OnStateChanged += TransitionToState;
                service.OnProgressChanged += UpdateProgress;
                service.OnProcessCompleted += CompleteProcess;
                service.OnBrewingCompleted += HandleBrewingCompleted;
            }
        }
        
        private void InitializeDropZoneTrackers()
        {
            for (int i = 0; i < portafilterZones.Count; i++)
            {
                if (portafilterZones[i] != null)
                {
                    var tracker = portafilterZones[i].GetComponent<DropZoneItemTracker>();
                    if (tracker == null)
                        tracker = portafilterZones[i].gameObject.AddComponent<DropZoneItemTracker>();
                }
            }
            
            for (int i = 0; i < cupZones.Count; i++)
            {
                if (cupZones[i] != null)
                {
                    var tracker = cupZones[i].GetComponent<DropZoneItemTracker>();
                    if (tracker == null)
                        tracker = cupZones[i].gameObject.AddComponent<DropZoneItemTracker>();
                }
            }
        }
        
        private void EnsureStateManagersOnAllItems()
        {
            var allPortafilters = FindObjectsOfType<Portafilter>();
            var allCups = FindObjectsOfType<Cup>();
            
            foreach (var portafilter in allPortafilters)
            {
                var stateManager = portafilter.GetComponent<DraggableStateManager>();
                if (stateManager == null)
                    stateManager = portafilter.gameObject.AddComponent<DraggableStateManager>();
            }
            
            foreach (var cup in allCups)
            {
                var stateManager = cup.GetComponent<DraggableStateManager>();
                if (stateManager == null)
                    stateManager = cup.gameObject.AddComponent<DraggableStateManager>();
            }
        }
        
        private void ConfigureDropZones()
        {
            for (int i = 0; i < portafilterZones.Count; i++)
            {
                if (portafilterZones[i] != null)
                {
                    portafilterZones[i].SlotIndex = i;
                }
            }
            
            for (int i = 0; i < cupZones.Count; i++)
            {
                if (cupZones[i] != null)
                {
                    cupZones[i].SlotIndex = i;
                }
            }
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            if (service != null)
            {
                service.OnStateChanged -= TransitionToState;
                service.OnProgressChanged -= UpdateProgress;
                service.OnProcessCompleted -= CompleteProcess;
                service.OnBrewingCompleted -= HandleBrewingCompleted;
            }
        }
        
        protected override void ApplyUpgrade(int previousLevel, int newLevel)
        {
            service?.SetUpgradeLevel(newLevel);
            
            switch (newLevel)
            {
                case 1:
                    NotifyUser("Faster brewing time!");
                    break;
                case 2:
                    NotifyUser("Extra brewing slots and auto-brewing activated!");
                    break;
            }
        }
        
        private void Update()
        {
            CheckSlotsPresence();
            
            var espressoService = CoreServices.Get<IEspressoMachineService>();
            if (espressoService != null)
                espressoService.UpdateBrewing(Time.deltaTime);
        }
        
        private void CheckSlotsPresence()
        {
            int maxSlots = upgradeLevel >= 2 ? portafilterZones.Count : 2;
            
            for (int i = 0; i < maxSlots; i++)
            {
                bool canUpdate = !lastStateUpdateTime.ContainsKey(i) || 
                                (Time.time - lastStateUpdateTime[i]) > STATE_UPDATE_COOLDOWN;
                
                CheckPortafilterInSlot(i, canUpdate);
                CheckCupInSlot(i, canUpdate);
            }
        }
        
        private void CheckPortafilterInSlot(int slotIndex, bool canUpdate)
        {
            if (slotIndex >= portafilterZones.Count || portafilterZones[slotIndex] == null) return;
            
            var tracker = portafilterZones[slotIndex].GetComponent<DropZoneItemTracker>();
            if (tracker == null) return;
            
            if (tracker.HasItem && !currentPortafilters.ContainsKey(slotIndex))
            {
                Portafilter portafilter = tracker.CurrentItem as Portafilter;
                if (portafilter != null)
                {
                    currentPortafilters[slotIndex] = portafilter;
                    if (canUpdate)
                    {
                        service?.SetPortafilter(slotIndex, true, portafilter.HasGroundCoffee, portafilter.GetCoffeeQualityFactor());
                        lastStateUpdateTime[slotIndex] = Time.time;
                    }
                }
            }
            else if (!tracker.HasItem && currentPortafilters.ContainsKey(slotIndex))
            {
                currentPortafilters.Remove(slotIndex);
                if (canUpdate)
                {
                    service?.SetPortafilter(slotIndex, false);
                    lastStateUpdateTime[slotIndex] = Time.time;
                }
            }
            else if (currentPortafilters.ContainsKey(slotIndex) && canUpdate)
            {
                var portafilter = currentPortafilters[slotIndex];
                var espressoService = CoreServices.Get<IEspressoMachineService>();
                var slot = espressoService?.GetSlot(slotIndex);
                
                if (slot != null)
                {
                    bool currentHasCoffee = portafilter.HasGroundCoffee;
                    float currentQuality = portafilter.GetCoffeeQualityFactor();
                    
                    if (slot.hasGroundCoffee != currentHasCoffee || 
                        (currentHasCoffee && Mathf.Abs(slot.coffeeQuality - currentQuality) > 0.01f))
                    {
                        service?.SetPortafilter(slotIndex, true, currentHasCoffee, currentQuality);
                        lastStateUpdateTime[slotIndex] = Time.time;
                    }
                }
            }
        }
        
        private void CheckCupInSlot(int slotIndex, bool canUpdate)
        {
            if (slotIndex >= cupZones.Count || cupZones[slotIndex] == null) return;
            
            var tracker = cupZones[slotIndex].GetComponent<DropZoneItemTracker>();
            if (tracker == null) return;
            
            if (tracker.HasItem && !currentCups.ContainsKey(slotIndex))
            {
                Cup cup = tracker.CurrentItem as Cup;
                if (cup != null)
                {
                    currentCups[slotIndex] = cup;
                    if (canUpdate)
                    {
                        service?.SetCup(slotIndex, true);
                        lastStateUpdateTime[slotIndex] = Time.time;
                    }
                }
            }
            else if (!tracker.HasItem && currentCups.ContainsKey(slotIndex))
            {
                currentCups.Remove(slotIndex);
                if (canUpdate)
                {
                    service?.SetCup(slotIndex, false);
                    lastStateUpdateTime[slotIndex] = Time.time;
                }
            }
        }
        
        public void OnBrewButtonClicked()
        {
            var espressoService = CoreServices.Get<IEspressoMachineService>();
            espressoService?.BrewAllReadySlots();
        }
        
        public override void StartProcess()
        {
            base.StartProcess();
            OnBrewButtonClicked();
        }
        
        private void HandleBrewingCompleted(int slotIndex)
        {
            if (!currentCups.ContainsKey(slotIndex) || !currentPortafilters.ContainsKey(slotIndex)) return;
            
            var cup = currentCups[slotIndex];
            var portafilter = currentPortafilters[slotIndex];
            
            var espressoService = CoreServices.Get<IEspressoMachineService>();
            var slot = espressoService?.GetSlot(slotIndex);
            
            if (cup != null && portafilter != null && slot != null)
            {
                float qualityFactor = slot.coffeeQuality;
                float shotAmount = 2f;
                float adjustedAmount = shotAmount * Mathf.Lerp(0.7f, 1.2f, qualityFactor);
                
                cup.TryAddItem("espresso", adjustedAmount);
                portafilter.Clear();
                
                string qualityDescription = GetQualityDescription(qualityFactor);
                NotifyUser($"{qualityDescription} espresso ready!");
            }
            
            RestoreSlotItemsToNormalState(slotIndex);
            StartCoroutine(ResetStateUpdateCooldown(slotIndex));
        }
        
        private void RestoreSlotItemsToNormalState(int slotIndex)
        {
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
            
            var stateManager = item.GetComponent<DraggableStateManager>();
            stateManager?.ForceReset();
        }
        
        private IEnumerator ResetStateUpdateCooldown(int slotIndex)
        {
            yield return new WaitForSeconds(0.5f);
            if (lastStateUpdateTime.ContainsKey(slotIndex))
                lastStateUpdateTime[slotIndex] = 0f;
        }
        
        private string GetQualityDescription(float qualityFactor)
        {
            if (qualityFactor >= 0.9f) return "Perfect";
            else if (qualityFactor >= 0.7f) return "Excellent";
            else if (qualityFactor >= 0.5f) return "Good";
            else if (qualityFactor >= 0.3f) return "Okay";
            else return "Poor";
        }
        
        public void OnPortafilterDropped(int slotIndex, Draggable item)
        {
            if (slotIndex >= 0 && slotIndex < portafilterZones.Count && item is Portafilter portafilter)
            {
                currentPortafilters[slotIndex] = portafilter;
                service?.SetPortafilter(slotIndex, true, portafilter.HasGroundCoffee, portafilter.GetCoffeeQualityFactor());
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
            if (slotIndex >= 0 && slotIndex < cupZones.Count && item is Cup cup)
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
        
        public bool CanBrewAnySlot()
        {
            return service?.CanBrewAnySlot() ?? false;
        }
        
        public int GetSlotCount()
        {
            return upgradeLevel >= 2 ? portafilterZones.Count : 2;
        }
        
        public Dictionary<int, Portafilter> GetCurrentPortafilters() => currentPortafilters;
        public Dictionary<int, Cup> GetCurrentCups() => currentCups;
        
        public EspressoMachineService GetService() => service;
    }
}
