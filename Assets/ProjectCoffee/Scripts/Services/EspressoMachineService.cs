using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectCoffee.Services.Interfaces;

namespace ProjectCoffee.Services
{
    /// <summary>
    /// Service for managing espresso machine state and logic
    /// </summary>
    public class EspressoMachineService : MachineService, IEspressoMachineService
    {
        public class BrewingSlot : IEspressoMachineService.IBrewingSlot
        {
            private bool _isActive = false;
            private float _brewProgress = 0f;
            private bool _hasPortafilter = false;
            private bool _hasCup = false;
            private bool _hasGroundCoffee = false;
            private float _coffeeQuality = 0f;
            
            public bool isActive { get => _isActive; set => _isActive = value; }
            public float brewProgress { get => _brewProgress; set => _brewProgress = value; }
            public bool hasPortafilter { get => _hasPortafilter; set => _hasPortafilter = value; }
            public bool hasCup { get => _hasCup; set => _hasCup = value; }
            public bool hasGroundCoffee { get => _hasGroundCoffee; set => _hasGroundCoffee = value; }
            public float coffeeQuality { get => _coffeeQuality; set => _coffeeQuality = value; }
        }

        // Events specific to espresso machine
        public event Action<int> OnSlotStateChanged;
        public event Action<int, float> OnSlotProgressChanged;
        public event Action<int> OnBrewingCompleted;
        
        private readonly Dictionary<int, BrewingSlot> slots = new Dictionary<int, BrewingSlot>();
        private float currentBrewTime;
    private float espressoQualityLevelBonus = 0f; // Quality bonus from machine upgrades
        
        public EspressoMachineService(EspressoMachineConfig config) : base(config)
        {
            // Initialize slots based on upgrade level
            UpdateSlotCount();
            UpdateBrewTimeFromConfig();
        }
        
        private void UpdateSlotCount()
        {
            int slotCount = 2; // Default for level 0
            if (upgradeLevel >= 2 && config is EspressoMachineConfig espressoConfig)
            {
                slotCount += espressoConfig.level2ExtraSlots;
            }
            
            // Initialize slots if they don't exist
            for (int i = 0; i < slotCount; i++)
            {
                if (!slots.ContainsKey(i))
                {
                    slots[i] = new BrewingSlot();
                }
            }
        }
        
        private void UpdateBrewTimeFromConfig()
        {
            if (config is EspressoMachineConfig espressoConfig)
            {
                currentBrewTime = upgradeLevel switch
                {
                    0 => espressoConfig.level0BrewTime,
                    1 => espressoConfig.level1BrewTime,
                    2 => espressoConfig.level2BrewTime,
                    _ => espressoConfig.level0BrewTime
                };
            }
            else
            {
                currentBrewTime = 5f; // Default fallback
            }
        }
        
        /// <summary>
        /// Set portafilter presence in a slot
        /// </summary>
        public void SetPortafilter(int slotIndex, bool present, bool hasGroundCoffee = false, float quality = 0f)
        {
            if (slots.TryGetValue(slotIndex, out BrewingSlot slot))
            {
                Debug.Log($"Setting portafilter in slot {slotIndex}: present={present}, hasGroundCoffee={hasGroundCoffee}, quality={quality}");
                
                slot.hasPortafilter = present;
                slot.hasGroundCoffee = hasGroundCoffee;
                slot.coffeeQuality = quality;
                
                OnSlotStateChanged?.Invoke(slotIndex);
                UpdateMachineState();
                
                // AUTO-BREW TRIGGER: If level 2 and all conditions met, start brewing immediately
                if (upgradeLevel == 2 && present && hasGroundCoffee && slot.hasCup && !slot.isActive)
                {
                    Debug.Log($"SetPortafilter: Auto-starting brew for slot {slotIndex} - all conditions met!");
                    StartBrewingSlot(slotIndex);
                }
            }
        }
        
        /// <summary>
        /// Set cup presence in a slot
        /// </summary>
        public void SetCup(int slotIndex, bool present)
        {
            if (slots.TryGetValue(slotIndex, out BrewingSlot slot))
            {
                Debug.Log($"Setting cup in slot {slotIndex}: present={present}");
                
                slot.hasCup = present;
                OnSlotStateChanged?.Invoke(slotIndex);
                UpdateMachineState();
                
                // AUTO-BREW TRIGGER: If level 2 and all conditions met, start brewing immediately
                if (upgradeLevel == 2 && present && slot.hasPortafilter && slot.hasGroundCoffee && !slot.isActive)
                {
                    Debug.Log($"SetCup: Auto-starting brew for slot {slotIndex} - all conditions met!");
                    StartBrewingSlot(slotIndex);
                }
            }
        }
        
        /// <summary>
        /// Check if a slot can brew
        /// </summary>
        public bool CanBrewSlot(int slotIndex)
        {
            if (slots.TryGetValue(slotIndex, out BrewingSlot slot))
            {
                bool canBrew = !slot.isActive && slot.hasPortafilter && slot.hasGroundCoffee && slot.hasCup;
                Debug.Log($"CanBrewSlot {slotIndex}: {canBrew} (hasPortafilter:{slot.hasPortafilter}, hasGroundCoffee:{slot.hasGroundCoffee}, hasCup:{slot.hasCup}, isActive:{slot.isActive})");
                return canBrew;
            }
            return false;
        }
        
        /// <summary>
        /// Start brewing on a specific slot
        /// </summary>
        public bool StartBrewingSlot(int slotIndex)
        {
            if (!CanBrewSlot(slotIndex))
                return false;
                
            if (slots.TryGetValue(slotIndex, out BrewingSlot slot))
            {
                slot.isActive = true;
                slot.brewProgress = 0f;
                OnSlotStateChanged?.Invoke(slotIndex);
                UpdateMachineState();
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Update brewing progress for all active slots
        /// </summary>
        public void UpdateBrewing(float deltaTime)
        {
            foreach (var kvp in slots)
            {
                int slotIndex = kvp.Key;
                BrewingSlot slot = kvp.Value;
                
                if (slot.isActive)
                {
                    slot.brewProgress += deltaTime / currentBrewTime;
                    OnSlotProgressChanged?.Invoke(slotIndex, slot.brewProgress);
                    
                    if (slot.brewProgress >= 1f)
                    {
                        CompleteBrewing(slotIndex);
                    }
                }
            }
        }
        
        /// <summary>
        /// Complete brewing on a slot
        /// </summary>
        private void CompleteBrewing(int slotIndex)
        {
            if (slots.TryGetValue(slotIndex, out BrewingSlot slot))
            {
                // Apply quality bonus from machine level
                if (upgradeLevel > 0)
                {
                    // At higher upgrade levels, espresso quality is improved
                    float baseQuality = slot.coffeeQuality;
                    float improvedQuality = Mathf.Clamp01(baseQuality + espressoQualityLevelBonus);
                    
                    // At level 2, quality is always at least 0.7 (good) regardless of input
                    if (upgradeLevel >= 2)
                    {
                        improvedQuality = Mathf.Max(improvedQuality, 0.7f);
                    }
                    
                    slot.coffeeQuality = improvedQuality;
                    Debug.Log($"Improving espresso quality from {baseQuality} to {improvedQuality} due to machine level {upgradeLevel}");
                }
                
                slot.isActive = false;
                slot.brewProgress = 0f;
                OnBrewingCompleted?.Invoke(slotIndex);
                OnSlotStateChanged?.Invoke(slotIndex);
                UpdateMachineState();
            }
        }
        
        /// <summary>
        /// Get slot information
        /// </summary>
        public IEspressoMachineService.IBrewingSlot GetSlot(int slotIndex)
        {
            return slots.TryGetValue(slotIndex, out BrewingSlot slot) ? slot : null;
        }
        
        /// <summary>
        /// Update overall machine state based on slots
        /// </summary>
        private void UpdateMachineState()
        {
            bool anyActive = false;
            bool anyReadyToBrew = false;
            List<int> readySlots = new List<int>();
            
            foreach (var entry in slots)
            {
                int slotIndex = entry.Key;
                var slot = entry.Value;
                
                if (slot.isActive)
                {
                    anyActive = true;
                }
                
                // Track slots that are ready to brew
                if (!slot.isActive && slot.hasPortafilter && slot.hasGroundCoffee && slot.hasCup)
                {
                    anyReadyToBrew = true;
                    readySlots.Add(slotIndex);
                }
            }
            
            if (anyActive)
            {
                TransitionTo(MachineState.Processing);
            }
            else if (anyReadyToBrew)
            {
                TransitionTo(MachineState.Ready);
            }
            else
            {
                TransitionTo(MachineState.Idle);
            }
        }
        
        /// <summary>
        /// Check if any slot can brew
        /// </summary>
        public bool CanBrewAnySlot()
        {
            foreach (var kvp in slots)
            {
                if (CanBrewSlot(kvp.Key))
                    return true;
            }
            return false;
        }
        
        /// <summary>
        /// Start brewing on all ready slots
        /// </summary>
        public void BrewAllReadySlots()
        {
            foreach (var kvp in slots)
            {
                if (CanBrewSlot(kvp.Key))
                {
                    StartBrewingSlot(kvp.Key);
                }
            }
        }
        
        protected override void OnUpgradeLevelChanged(int level)
        {
            UpdateSlotCount();
            UpdateBrewTimeFromConfig();
            
            // Log the level change and the slots info for debugging
            Debug.Log($"Espresso machine upgrade level changed to {level}. Available slots: {slots.Count}");
            foreach (var entry in slots)
            {
                Debug.Log($"Slot {entry.Key}: hasPortafilter={entry.Value.hasPortafilter}, hasGroundCoffee={entry.Value.hasGroundCoffee}, hasCup={entry.Value.hasCup}");
            }
            
            switch (level)
            {
                case 0:
                    NotifyUser("Basic espresso machine - 2 slots");
                    break;
                case 1:
                    NotifyUser("Faster brewing time!");
                    break;
                case 2:
                    NotifyUser("Extra brewing slots and auto-brewing activated!");
                    break;
            }
            
            // Update quality factor based on upgrade level
            espressoQualityLevelBonus = level * 0.1f; // Each level adds 10% quality bonus
            
            // Trigger state update to enable auto-brewing if needed
            UpdateMachineState();
        }
    }
}
