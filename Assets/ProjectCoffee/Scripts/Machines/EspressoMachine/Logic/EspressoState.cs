using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectCoffee.Services;

namespace ProjectCoffee.Machines.EspressoMachine.Logic
{
    /// <summary>
    /// Centralized state management for espresso machine.
    /// Handles slot states, brewing progress, and state transitions.
    /// </summary>
    public class EspressoState
    {
        #region Events
        
        public event Action<int> OnSlotStateChanged;
        public event Action<int, float> OnSlotProgressChanged;
        public event Action<int> OnBrewingStarted;
        public event Action<int> OnBrewingCompleted;
        public event Action<MachineState> OnMachineStateChanged;
        
        #endregion
        
        #region Private Fields
        
        private readonly Dictionary<int, SlotData> slots = new Dictionary<int, SlotData>();
        private MachineState currentMachineState = MachineState.Idle;
        private int upgradeLevel = 0;
        private readonly EspressoLogic logic;
        
        #endregion
        
        #region Properties
        
        public MachineState CurrentMachineState => currentMachineState;
        public int UpgradeLevel => upgradeLevel;
        public int ActiveSlotCount => GetActiveSlotCount();
        public int ReadySlotCount => GetReadySlotCount();
        
        #endregion
        
        public EspressoState(EspressoLogic logic)
        {
            this.logic = logic ?? throw new ArgumentNullException(nameof(logic));
            InitializeSlots();
        }
        
        #region Initialization
        
        private void InitializeSlots()
        {
            // Initialize with maximum possible slots
            for (int i = 0; i < 4; i++)
            {
                slots[i] = new SlotData();
            }
        }
        
        #endregion
        
        #region Slot Management
        
        /// <summary>
        /// Sets portafilter presence in a slot
        /// </summary>
        public void SetPortafilter(int slotIndex, bool present, bool hasGroundCoffee = false, float coffeeQuality = 0f)
        {
            if (!slots.TryGetValue(slotIndex, out SlotData slot))
                return;
                
            bool changed = slot.HasPortafilter != present || 
                          slot.HasGroundCoffee != hasGroundCoffee || 
                          Mathf.Abs(slot.CoffeeQuality - coffeeQuality) > 0.01f;
            
            if (changed)
            {
                slot.HasPortafilter = present;
                slot.HasGroundCoffee = hasGroundCoffee;
                slot.CoffeeQuality = coffeeQuality;
                
                NotifySlotChanged(slotIndex);
                UpdateMachineState();
                CheckAutoBrewTrigger(slotIndex);
            }
        }
        
        /// <summary>
        /// Sets cup presence in a slot
        /// </summary>
        public void SetCup(int slotIndex, bool present)
        {
            if (!slots.TryGetValue(slotIndex, out SlotData slot))
                return;
                
            if (slot.HasCup != present)
            {
                slot.HasCup = present;
                NotifySlotChanged(slotIndex);
                UpdateMachineState();
                CheckAutoBrewTrigger(slotIndex);
            }
        }
        
        /// <summary>
        /// Gets slot data for a specific slot
        /// </summary>
        public SlotData GetSlot(int slotIndex)
        {
            return slots.TryGetValue(slotIndex, out SlotData slot) ? slot : null;
        }
        
        /// <summary>
        /// Gets all slots as logic slot states for validation
        /// </summary>
        public Dictionary<int, EspressoLogic.SlotState> GetLogicSlotStates()
        {
            var logicStates = new Dictionary<int, EspressoLogic.SlotState>();
            
            foreach (var kvp in slots)
            {
                var slotData = kvp.Value;
                logicStates[kvp.Key] = new EspressoLogic.SlotState(
                    slotData.HasPortafilter,
                    slotData.HasCup,
                    slotData.HasGroundCoffee,
                    slotData.IsActive,
                    slotData.CoffeeQuality
                );
            }
            
            return logicStates;
        }
        
        #endregion
        
        #region Brewing Management
        
        /// <summary>
        /// Starts brewing on a specific slot
        /// </summary>
        public bool StartBrewing(int slotIndex)
        {
            if (!slots.TryGetValue(slotIndex, out SlotData slot))
                return false;
                
            if (!logic.CanSlotBrew(slot.HasPortafilter, slot.HasCup, slot.HasGroundCoffee, slot.IsActive))
                return false;
            
            slot.IsActive = true;
            slot.BrewProgress = 0f;
            slot.BrewStartTime = Time.time;
            
            OnBrewingStarted?.Invoke(slotIndex);
            NotifySlotChanged(slotIndex);
            UpdateMachineState();
            
            return true;
        }
        
        /// <summary>
        /// Updates brewing progress for all active slots
        /// </summary>
        public void UpdateBrewingProgress(float deltaTime)
        {
            foreach (var kvp in slots)
            {
                int slotIndex = kvp.Key;
                var slot = kvp.Value;
                
                if (slot.IsActive)
                {
                    float elapsedTime = Time.time - slot.BrewStartTime;
                    float newProgress = logic.CalculateBrewingProgress(elapsedTime, upgradeLevel);
                    
                    if (Mathf.Abs(slot.BrewProgress - newProgress) > 0.01f)
                    {
                        slot.BrewProgress = newProgress;
                        OnSlotProgressChanged?.Invoke(slotIndex, newProgress);
                    }
                    
                    if (logic.IsBrewingComplete(newProgress))
                    {
                        CompleteBrewing(slotIndex);
                    }
                }
            }
        }
        
        /// <summary>
        /// Completes brewing for a specific slot
        /// </summary>
        public void CompleteBrewing(int slotIndex)
        {
            if (!slots.TryGetValue(slotIndex, out SlotData slot))
                return;
                
            if (!slot.IsActive)
                return;
            
            // Calculate final quality with upgrade bonuses
            float finalQuality = logic.CalculateEspressoQuality(slot.CoffeeQuality, upgradeLevel);
            slot.FinalEspressoQuality = finalQuality;
            
            slot.IsActive = false;
            slot.BrewProgress = 0f;
            
            OnBrewingCompleted?.Invoke(slotIndex);
            NotifySlotChanged(slotIndex);
            UpdateMachineState();
        }
        
        /// <summary>
        /// Checks if any slots can start brewing
        /// </summary>
        public bool CanBrewAnySlot()
        {
            var validation = logic.ValidateBrewingConditions(upgradeLevel, GetLogicSlotStates());
            return validation.HasReadySlots;
        }
        
        /// <summary>
        /// Starts brewing on all ready slots
        /// </summary>
        public List<int> StartBrewingAllReady()
        {
            var validation = logic.ValidateBrewingConditions(upgradeLevel, GetLogicSlotStates());
            var startedSlots = new List<int>();
            
            foreach (int slotIndex in validation.ReadySlots)
            {
                if (logic.IsValidSlotIndex(slotIndex, upgradeLevel) && StartBrewing(slotIndex))
                {
                    startedSlots.Add(slotIndex);
                }
            }
            
            return startedSlots;
        }
        
        #endregion
        
        #region State Management
        
        /// <summary>
        /// Sets the upgrade level and updates slot availability
        /// </summary>
        public void SetUpgradeLevel(int level)
        {
            if (upgradeLevel != level)
            {
                upgradeLevel = level;
                UpdateMachineState();
            }
        }
        
        /// <summary>
        /// Updates overall machine state based on slot conditions
        /// </summary>
        private void UpdateMachineState()
        {
            MachineState newState = DetermineMachineState();
            
            if (currentMachineState != newState)
            {
                currentMachineState = newState;
                OnMachineStateChanged?.Invoke(newState);
            }
        }
        
        /// <summary>
        /// Determines appropriate machine state based on current slot conditions
        /// </summary>
        private MachineState DetermineMachineState()
        {
            int activeSlots = GetActiveSlotCount();
            int readySlots = GetReadySlotCount();
            
            if (activeSlots > 0)
                return MachineState.Processing;
            else if (readySlots > 0)
                return MachineState.Ready;
            else
                return MachineState.Idle;
        }
        
        #endregion
        
        #region Auto-Brewing Logic
        
        /// <summary>
        /// Checks if auto-brewing should trigger for a slot
        /// </summary>
        private void CheckAutoBrewTrigger(int slotIndex)
        {
            if (!logic.IsValidSlotIndex(slotIndex, upgradeLevel))
                return;
                
            if (!slots.TryGetValue(slotIndex, out SlotData slot))
                return;
            
            if (logic.ShouldAutoBrewSlot(upgradeLevel, slot.HasPortafilter, slot.HasCup, slot.HasGroundCoffee, slot.IsActive))
            {
                StartBrewing(slotIndex);
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        private int GetActiveSlotCount()
        {
            int count = 0;
            int availableSlots = logic.GetAvailableSlotCount(upgradeLevel);
            
            for (int i = 0; i < availableSlots; i++)
            {
                if (slots.TryGetValue(i, out SlotData slot) && slot.IsActive)
                    count++;
            }
            
            return count;
        }
        
        private int GetReadySlotCount()
        {
            int count = 0;
            int availableSlots = logic.GetAvailableSlotCount(upgradeLevel);
            
            for (int i = 0; i < availableSlots; i++)
            {
                if (slots.TryGetValue(i, out SlotData slot) && 
                    logic.CanSlotBrew(slot.HasPortafilter, slot.HasCup, slot.HasGroundCoffee, slot.IsActive))
                {
                    count++;
                }
            }
            
            return count;
        }
        
        private void NotifySlotChanged(int slotIndex)
        {
            OnSlotStateChanged?.Invoke(slotIndex);
        }
        
        #endregion
        
        #region Reset and Cleanup
        
        /// <summary>
        /// Resets all slots to idle state
        /// </summary>
        public void Reset()
        {
            foreach (var slot in slots.Values)
            {
                slot.Reset();
            }
            
            currentMachineState = MachineState.Idle;
            OnMachineStateChanged?.Invoke(currentMachineState);
        }
        
        #endregion
        
        #region Slot Data Class
        
        /// <summary>
        /// Data container for a single brewing slot
        /// </summary>
        public class SlotData
        {
            public bool HasPortafilter { get; set; }
            public bool HasCup { get; set; }
            public bool HasGroundCoffee { get; set; }
            public bool IsActive { get; set; }
            public float CoffeeQuality { get; set; }
            public float BrewProgress { get; set; }
            public float BrewStartTime { get; set; }
            public float FinalEspressoQuality { get; set; }
            
            public void Reset()
            {
                HasPortafilter = false;
                HasCup = false;
                HasGroundCoffee = false;
                IsActive = false;
                CoffeeQuality = 0f;
                BrewProgress = 0f;
                BrewStartTime = 0f;
                FinalEspressoQuality = 0f;
            }
        }
        
        #endregion
    }
}
