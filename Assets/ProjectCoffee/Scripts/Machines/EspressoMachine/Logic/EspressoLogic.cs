using UnityEngine;
using ProjectCoffee.Services;

namespace ProjectCoffee.Machines.EspressoMachine.Logic
{
    /// <summary>
    /// Contains all espresso machine business logic separated from Unity dependencies.
    /// Handles brewing calculations, slot validation, upgrade effects, and timing logic.
    /// </summary>
    public class EspressoLogic
    {
        private readonly EspressoMachineConfig config;
        
        public EspressoLogic(EspressoMachineConfig config)
        {
            this.config = config ?? throw new System.ArgumentNullException(nameof(config));
        }
        
        #region Slot Management Logic
        
        /// <summary>
        /// Determines how many slots should be available based on upgrade level
        /// Level 0&1: 2 slots, Level 2: 4 slots
        /// </summary>
        public int GetAvailableSlotCount(int upgradeLevel)
        {
            return upgradeLevel >= 2 ? 4 : 2;
        }
        
        /// <summary>
        /// Validates if a slot can start brewing based on its contents
        /// </summary>
        public bool CanSlotBrew(bool hasPortafilter, bool hasCup, bool hasGroundCoffee, bool isCurrentlyBrewing)
        {
            return !isCurrentlyBrewing && hasPortafilter && hasCup && hasGroundCoffee;
        }
        
        /// <summary>
        /// Determines if auto-brewing should trigger for a slot at the current upgrade level
        /// </summary>
        public bool ShouldAutoBrewSlot(int upgradeLevel, bool hasPortafilter, bool hasCup, bool hasGroundCoffee, bool isCurrentlyBrewing)
        {
            if (upgradeLevel < 2 || !config.level2EnableAutoBrewing)
                return false;
                
            return CanSlotBrew(hasPortafilter, hasCup, hasGroundCoffee, isCurrentlyBrewing);
        }
        
        #endregion
        
        #region Brewing Logic
        
        /// <summary>
        /// Gets the brewing time for the current upgrade level
        /// </summary>
        public float GetBrewingTime(int upgradeLevel)
        {
            return upgradeLevel switch
            {
                0 => config.level0BrewTime,
                1 => config.level1BrewTime,
                2 => config.level2BrewTime,
                _ => config.level0BrewTime
            };
        }
        
        /// <summary>
        /// Calculates brewing progress based on elapsed time
        /// </summary>
        public float CalculateBrewingProgress(float elapsedTime, int upgradeLevel)
        {
            float brewTime = GetBrewingTime(upgradeLevel);
            return brewTime > 0 ? Mathf.Clamp01(elapsedTime / brewTime) : 1f;
        }
        
        /// <summary>
        /// Determines if brewing is complete based on progress
        /// </summary>
        public bool IsBrewingComplete(float progress)
        {
            return progress >= 1f;
        }
        
        #endregion
        
        #region Quality Calculations
        
        /// <summary>
        /// Calculates final espresso quality based on coffee quality and upgrade level
        /// </summary>
        public float CalculateEspressoQuality(float coffeeQuality, int upgradeLevel)
        {
            float baseQuality = coffeeQuality * config.coffeeQualityWeight;
            float upgradeBonus = GetUpgradeQualityBonus(upgradeLevel);
            float finalQuality = Mathf.Clamp01(baseQuality + upgradeBonus);
            
            // Level 2 ensures minimum quality regardless of input
            if (upgradeLevel >= 2)
            {
                finalQuality = Mathf.Max(finalQuality, 0.7f);
            }
            
            return finalQuality;
        }
        
        /// <summary>
        /// Gets quality bonus from machine upgrade level
        /// </summary>
        public float GetUpgradeQualityBonus(int upgradeLevel)
        {
            return upgradeLevel * 0.1f; // 10% bonus per level
        }
        
        /// <summary>
        /// Calculates espresso amount based on quality (variable yield based on extraction)
        /// </summary>
        public float CalculateEspressoAmount(float qualityFactor)
        {
            float baseAmount = 2f; // Standard double shot
            return baseAmount * Mathf.Lerp(0.7f, 1.2f, qualityFactor);
        }
        
        /// <summary>
        /// Gets quality description for UI display
        /// </summary>
        public string GetQualityDescription(float qualityFactor)
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
        
        #region Upgrade Logic
        
        /// <summary>
        /// Gets interaction type based on upgrade level
        /// </summary>
        public InteractionType GetInteractionType(int upgradeLevel)
        {
            return upgradeLevel switch
            {
                0 => InteractionType.ButtonPress,
                1 => InteractionType.ButtonPress,
                2 => InteractionType.AutoProcess,
                _ => InteractionType.ButtonPress
            };
        }
        
        /// <summary>
        /// Determines if manual brewing is required for the current upgrade level
        /// </summary>
        public bool RequiresManualBrewing(int upgradeLevel)
        {
            return upgradeLevel < 2;
        }
        
        /// <summary>
        /// Gets upgrade-specific features description
        /// </summary>
        public string GetUpgradeDescription(int upgradeLevel)
        {
            return upgradeLevel switch
            {
                0 => config.level0Description,
                1 => config.level1Description,
                2 => config.level2Description,
                _ => $"Upgrade level {upgradeLevel}"
            };
        }
        
        #endregion
        
        #region Validation Logic
        
        /// <summary>
        /// Validates slot index is within valid range for upgrade level
        /// </summary>
        public bool IsValidSlotIndex(int slotIndex, int upgradeLevel)
        {
            return slotIndex >= 0 && slotIndex < GetAvailableSlotCount(upgradeLevel);
        }
        
        /// <summary>
        /// Validates brewing can start across all provided slots
        /// </summary>
        public BrewingValidationResult ValidateBrewingConditions(int upgradeLevel, System.Collections.Generic.Dictionary<int, SlotState> slotStates)
        {
            var result = new BrewingValidationResult();
            int availableSlots = GetAvailableSlotCount(upgradeLevel);
            
            foreach (var kvp in slotStates)
            {
                int slotIndex = kvp.Key;
                var slotState = kvp.Value;
                
                if (!IsValidSlotIndex(slotIndex, upgradeLevel))
                {
                    result.AddError(slotIndex, "Slot not available at current upgrade level");
                    continue;
                }
                
                if (CanSlotBrew(slotState.HasPortafilter, slotState.HasCup, slotState.HasGroundCoffee, slotState.IsActive))
                {
                    result.AddReadySlot(slotIndex);
                }
                else if (slotState.IsActive)
                {
                    result.AddActiveSlot(slotIndex);
                }
            }
            
            return result;
        }
        
        #endregion
        
        #region Helper Classes
        
        /// <summary>
        /// Represents the state of a single brewing slot
        /// </summary>
        public class SlotState
        {
            public bool HasPortafilter { get; set; }
            public bool HasCup { get; set; }
            public bool HasGroundCoffee { get; set; }
            public bool IsActive { get; set; }
            public float CoffeeQuality { get; set; }
            public float BrewProgress { get; set; }
            
            public SlotState(bool hasPortafilter = false, bool hasCup = false, bool hasGroundCoffee = false, bool isActive = false, float coffeeQuality = 0f)
            {
                HasPortafilter = hasPortafilter;
                HasCup = hasCup;
                HasGroundCoffee = hasGroundCoffee;
                IsActive = isActive;
                CoffeeQuality = coffeeQuality;
                BrewProgress = 0f;
            }
        }
        
        /// <summary>
        /// Result of brewing validation across all slots
        /// </summary>
        public class BrewingValidationResult
        {
            public System.Collections.Generic.List<int> ReadySlots { get; } = new System.Collections.Generic.List<int>();
            public System.Collections.Generic.List<int> ActiveSlots { get; } = new System.Collections.Generic.List<int>();
            public System.Collections.Generic.Dictionary<int, string> Errors { get; } = new System.Collections.Generic.Dictionary<int, string>();
            
            public bool HasReadySlots => ReadySlots.Count > 0;
            public bool HasActiveSlots => ActiveSlots.Count > 0;
            public bool HasErrors => Errors.Count > 0;
            
            public void AddReadySlot(int slotIndex) => ReadySlots.Add(slotIndex);
            public void AddActiveSlot(int slotIndex) => ActiveSlots.Add(slotIndex);
            public void AddError(int slotIndex, string error) => Errors[slotIndex] = error;
        }
        
        #endregion
    }
}
