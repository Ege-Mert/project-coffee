using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectCoffee.Services.Interfaces;
using ProjectCoffee.Machines.EspressoMachine.Logic;

namespace ProjectCoffee.Services
{
    /// <summary>
    /// Refactored service for managing espresso machine state and logic.
    /// Now uses EspressoLogic and EspressoState for clean separation of concerns.
    /// </summary>
    public class EspressoMachineService : MachineService, IEspressoMachineService
    {
        #region Legacy Interface Support
        
        /// <summary>
        /// Legacy brewing slot implementation for backward compatibility
        /// </summary>
        public class BrewingSlot : IEspressoMachineService.IBrewingSlot
        {
            private readonly Machines.EspressoMachine.Logic.EspressoState.SlotData slotData;
            
            public BrewingSlot(Machines.EspressoMachine.Logic.EspressoState.SlotData slotData)
            {
                this.slotData = slotData ?? throw new ArgumentNullException(nameof(slotData));
            }
            
            public bool isActive 
            { 
                get => slotData.IsActive; 
                set => slotData.IsActive = value; 
            }
            
            public float brewProgress 
            { 
                get => slotData.BrewProgress; 
                set => slotData.BrewProgress = value; 
            }
            
            public bool hasPortafilter 
            { 
                get => slotData.HasPortafilter; 
                set => slotData.HasPortafilter = value; 
            }
            
            public bool hasCup 
            { 
                get => slotData.HasCup; 
                set => slotData.HasCup = value; 
            }
            
            public bool hasGroundCoffee 
            { 
                get => slotData.HasGroundCoffee; 
                set => slotData.HasGroundCoffee = value; 
            }
            
            public float coffeeQuality 
            { 
                get => slotData.CoffeeQuality; 
                set => slotData.CoffeeQuality = value; 
            }
        }
        
        #endregion
        
        #region Events
        
        // Events specific to espresso machine - maintained for backward compatibility
        public event Action<int> OnSlotStateChanged;
        public event Action<int, float> OnSlotProgressChanged;
        public event Action<int> OnBrewingCompleted;
        
        #endregion
        
        #region Private Fields
        
        private readonly EspressoLogic logic;
        private readonly EspressoState state;
        private readonly Dictionary<int, BrewingSlot> legacySlots = new Dictionary<int, BrewingSlot>();
        
        #endregion
        
        #region Constructor and Initialization
        
        public EspressoMachineService(EspressoMachineConfig config) : base(config)
        {
            // Initialize logic and state
            logic = new EspressoLogic(config);
            state = new EspressoState(logic);
            
            // Subscribe to state events
            SubscribeToStateEvents();
            
            // Initialize with current upgrade level
            state.SetUpgradeLevel(upgradeLevel);
            
            // Initialize legacy slot wrappers
            InitializeLegacySlots();
            
            Debug.Log($"EspressoMachineService: Initialized with new logic and state system");
        }
        
        private void SubscribeToStateEvents()
        {
            state.OnSlotStateChanged += HandleSlotStateChanged;
            state.OnSlotProgressChanged += HandleSlotProgressChanged;
            state.OnBrewingCompleted += HandleBrewingCompleted;
            state.OnMachineStateChanged += HandleMachineStateChanged;
        }
        
        private void InitializeLegacySlots()
        {
            // Create legacy slot wrappers for backward compatibility
            int maxSlots = logic.GetAvailableSlotCount(2); // Max possible slots
            
            for (int i = 0; i < maxSlots; i++)
            {
                var slotData = state.GetSlot(i);
                if (slotData != null)
                {
                    legacySlots[i] = new BrewingSlot(slotData);
                }
            }
            
            Debug.Log($"EspressoMachineService: Initialized {legacySlots.Count} legacy slot wrappers");
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleSlotStateChanged(int slotIndex)
        {
            Debug.Log($"EspressoMachineService: Slot {slotIndex} state changed");
            OnSlotStateChanged?.Invoke(slotIndex);
        }
        
        private void HandleSlotProgressChanged(int slotIndex, float progress)
        {
            OnSlotProgressChanged?.Invoke(slotIndex, progress);
        }
        
        private void HandleBrewingCompleted(int slotIndex)
        {
            var slotData = state.GetSlot(slotIndex);
            if (slotData != null)
            {
                string qualityDesc = logic.GetQualityDescription(slotData.FinalEspressoQuality);
                Debug.Log($"EspressoMachineService: Brewing completed on slot {slotIndex} - {qualityDesc} quality ({slotData.FinalEspressoQuality:F2})");
            }
            
            OnBrewingCompleted?.Invoke(slotIndex);
        }
        
        private void HandleMachineStateChanged(MachineState newState)
        {
            TransitionTo(newState);
            Debug.Log($"EspressoMachineService: Machine state changed to {newState}");
        }
        
        #endregion
        
        #region Public Interface Methods
        
        /// <summary>
        /// Set portafilter presence in a slot
        /// </summary>
        public void SetPortafilter(int slotIndex, bool present, bool hasGroundCoffee = false, float quality = 0f)
        {
            if (!logic.IsValidSlotIndex(slotIndex, upgradeLevel))
            {
                Debug.LogWarning($"EspressoMachineService: Invalid slot index {slotIndex} for upgrade level {upgradeLevel}");
                return;
            }
            
            Debug.Log($"EspressoMachineService: Setting portafilter in slot {slotIndex}: present={present}, hasGroundCoffee={hasGroundCoffee}, quality={quality:F2}");
            
            state.SetPortafilter(slotIndex, present, hasGroundCoffee, quality);
        }
        
        /// <summary>
        /// Set cup presence in a slot
        /// </summary>
        public void SetCup(int slotIndex, bool present)
        {
            if (!logic.IsValidSlotIndex(slotIndex, upgradeLevel))
            {
                Debug.LogWarning($"EspressoMachineService: Invalid slot index {slotIndex} for upgrade level {upgradeLevel}");
                return;
            }
            
            Debug.Log($"EspressoMachineService: Setting cup in slot {slotIndex}: present={present}");
            
            state.SetCup(slotIndex, present);
        }
        
        /// <summary>
        /// Check if a slot can brew
        /// </summary>
        public bool CanBrewSlot(int slotIndex)
        {
            var slotData = state.GetSlot(slotIndex);
            if (slotData == null)
                return false;
            
            bool canBrew = logic.CanSlotBrew(slotData.HasPortafilter, slotData.HasCup, slotData.HasGroundCoffee, slotData.IsActive);
            
            Debug.Log($"EspressoMachineService: CanBrewSlot {slotIndex}: {canBrew} " +
                     $"(hasPortafilter:{slotData.HasPortafilter}, hasGroundCoffee:{slotData.HasGroundCoffee}, " +
                     $"hasCup:{slotData.HasCup}, isActive:{slotData.IsActive})");
            
            return canBrew;
        }
        
        /// <summary>
        /// Start brewing on a specific slot
        /// </summary>
        public bool StartBrewingSlot(int slotIndex)
        {
            if (!logic.IsValidSlotIndex(slotIndex, upgradeLevel))
            {
                Debug.LogWarning($"EspressoMachineService: Cannot start brewing - invalid slot {slotIndex}");
                return false;
            }
            
            bool started = state.StartBrewing(slotIndex);
            
            if (started)
            {
                Debug.Log($"EspressoMachineService: Started brewing on slot {slotIndex}");
            }
            else
            {
                Debug.Log($"EspressoMachineService: Failed to start brewing on slot {slotIndex} - conditions not met");
            }
            
            return started;
        }
        
        /// <summary>
        /// Update brewing progress for all active slots
        /// </summary>
        public void UpdateBrewing(float deltaTime)
        {
            state.UpdateBrewingProgress(deltaTime);
        }
        
        /// <summary>
        /// Get slot information (legacy interface support)
        /// </summary>
        public IEspressoMachineService.IBrewingSlot GetSlot(int slotIndex)
        {
            return legacySlots.TryGetValue(slotIndex, out BrewingSlot slot) ? slot : null;
        }
        
        /// <summary>
        /// Check if any slot can brew
        /// </summary>
        public bool CanBrewAnySlot()
        {
            bool canBrew = state.CanBrewAnySlot();
            Debug.Log($"EspressoMachineService: CanBrewAnySlot: {canBrew} (Ready slots: {state.ReadySlotCount})");
            return canBrew;
        }
        
        /// <summary>
        /// Start brewing on all ready slots
        /// </summary>
        public void BrewAllReadySlots()
        {
            var startedSlots = state.StartBrewingAllReady();
            
            if (startedSlots.Count > 0)
            {
                Debug.Log($"EspressoMachineService: Started brewing on {startedSlots.Count} slots: [{string.Join(", ", startedSlots)}]");
            }
            else
            {
                Debug.Log("EspressoMachineService: No slots ready for brewing");
            }
        }
        
        #endregion
        
        #region Upgrade Handling
        
        protected override void OnUpgradeLevelChanged(int level)
        {
            Debug.Log($"EspressoMachineService: Upgrade level changed to {level}");
            
            // Update state with new upgrade level
            state.SetUpgradeLevel(level);
            
            // Get upgrade description from logic
            string description = logic.GetUpgradeDescription(level);
            NotifyUser(description);
            
            // Log available slots for debugging
            int availableSlots = logic.GetAvailableSlotCount(level);
            Debug.Log($"EspressoMachineService: Available slots after upgrade: {availableSlots}");
            
            // Log current slot states
            for (int i = 0; i < availableSlots; i++)
            {
                var slotData = state.GetSlot(i);
                if (slotData != null)
                {
                    Debug.Log($"EspressoMachineService: Slot {i}: " +
                             $"hasPortafilter={slotData.HasPortafilter}, " +
                             $"hasGroundCoffee={slotData.HasGroundCoffee}, " +
                             $"hasCup={slotData.HasCup}, " +
                             $"isActive={slotData.IsActive}");
                }
            }
        }
        
        #endregion
        
        #region Cleanup
        
        public override void Reset()
        {
            base.Reset();
            state.Reset();
            Debug.Log("EspressoMachineService: Reset completed");
        }
        
        #endregion
        
        #region Utility Methods for Debugging
        
        /// <summary>
        /// Gets detailed state information for debugging
        /// </summary>
        public string GetDebugInfo()
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine($"EspressoMachineService Debug Info:");
            info.AppendLine($"- Upgrade Level: {upgradeLevel}");
            info.AppendLine($"- Machine State: {state.CurrentMachineState}");
            info.AppendLine($"- Available Slots: {logic.GetAvailableSlotCount(upgradeLevel)}");
            info.AppendLine($"- Ready Slots: {state.ReadySlotCount}");
            info.AppendLine($"- Active Slots: {state.ActiveSlotCount}");
            info.AppendLine($"- Interaction Type: {logic.GetInteractionType(upgradeLevel)}");
            info.AppendLine($"- Brewing Time: {logic.GetBrewingTime(upgradeLevel):F1}s");
            
            return info.ToString();
        }
        
        #endregion
    }
}
