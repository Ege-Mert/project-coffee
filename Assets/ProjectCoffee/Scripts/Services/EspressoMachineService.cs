using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectCoffee.Services
{
    /// <summary>
    /// Service for managing espresso machine state and logic
    /// </summary>
    public class EspressoMachineService : MachineService
    {
        public class BrewingSlot
        {
            public bool isActive = false;
            public float brewProgress = 0f;
            public bool hasPortafilter = false;
            public bool hasCup = false;
            public bool hasGroundCoffee = false;
            public float coffeeQuality = 0f;
        }

        // Events specific to espresso machine
        public event Action<int> OnSlotStateChanged;
        public event Action<int, float> OnSlotProgressChanged;
        public event Action<int> OnBrewingCompleted;
        
        private readonly Dictionary<int, BrewingSlot> slots = new Dictionary<int, BrewingSlot>();
        private float currentBrewTime;
        
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
                slot.hasPortafilter = present;
                slot.hasGroundCoffee = hasGroundCoffee;
                slot.coffeeQuality = quality;
                OnSlotStateChanged?.Invoke(slotIndex);
                UpdateMachineState();
            }
        }
        
        /// <summary>
        /// Set cup presence in a slot
        /// </summary>
        public void SetCup(int slotIndex, bool present)
        {
            if (slots.TryGetValue(slotIndex, out BrewingSlot slot))
            {
                slot.hasCup = present;
                OnSlotStateChanged?.Invoke(slotIndex);
                UpdateMachineState();
            }
        }
        
        /// <summary>
        /// Check if a slot can brew
        /// </summary>
        public bool CanBrewSlot(int slotIndex)
        {
            if (slots.TryGetValue(slotIndex, out BrewingSlot slot))
            {
                return !slot.isActive && slot.hasPortafilter && slot.hasGroundCoffee && slot.hasCup;
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
        public BrewingSlot GetSlot(int slotIndex)
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
            
            foreach (var slot in slots.Values)
            {
                if (slot.isActive)
                {
                    anyActive = true;
                    break;
                }
                if (!slot.isActive && slot.hasPortafilter && slot.hasGroundCoffee && slot.hasCup)
                {
                    anyReadyToBrew = true;
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
            
            switch (level)
            {
                case 0:
                    NotifyUser("Basic espresso machine - 2 slots");
                    break;
                case 1:
                    NotifyUser("Faster brewing time!");
                    break;
                case 2:
                    NotifyUser("Extra brewing slots added!");
                    break;
            }
        }
    }
}
