using ProjectCoffee.Core;

namespace ProjectCoffee.Machines.Dosing.Logic
{
    /// <summary>
    /// Pure business logic for coffee dosing operations.
    /// No Unity dependencies - fully testable.
    /// </summary>
    public class DosingLogic
    {
        private readonly DosingMachineConfig config;
        private readonly CoffeeQualityEvaluator qualityEvaluator;

        public DosingLogic(DosingMachineConfig config)
        {
            this.config = config;
            this.qualityEvaluator = new CoffeeQualityEvaluator(
                config.idealGramAmount,
                config.gramTolerance
            );
        }

        #region Coffee Management

        /// <summary>
        /// Calculate how much coffee can be added to storage
        /// </summary>
        public float CalculateAddableAmount(float currentStorage, float amountToAdd)
        {
            float availableSpace = config.maxStorageCapacity - currentStorage;
            return UnityEngine.Mathf.Min(amountToAdd, availableSpace);
        }

        /// <summary>
        /// Check if storage can accept coffee
        /// </summary>
        public bool CanAddCoffeeToStorage(float currentStorage, float amountToAdd)
        {
            return currentStorage < config.maxStorageCapacity && amountToAdd > 0;
        }

        /// <summary>
        /// Calculate dispensing rate for manual dosing
        /// </summary>
        public float CalculateDispenseAmount(float deltaTime, int upgradeLevel)
        {
            float baseRate = config.grammingRate;
            float multiplier = upgradeLevel == 0 ? config.level0HoldFactor : 1f;
            return baseRate * multiplier * deltaTime;
        }

        #endregion

        #region Auto-Dosing Logic

        /// <summary>
        /// Determine if auto-dosing should occur for level 2
        /// </summary>
        public bool ShouldAutoDose(int upgradeLevel, bool hasPortafilter, float storageAmount, float portafilterAmount)
        {
            if (upgradeLevel != 2) return false;
            if (!hasPortafilter) return false;
            if (storageAmount <= 0) return false;

            float amountNeeded = config.idealGramAmount - portafilterAmount;
            return amountNeeded > 0;
        }

        /// <summary>
        /// Calculate the ideal dose amount for auto-dosing
        /// </summary>
        public DosingCalculation CalculateAutoDose(float currentPortafilterAmount, float availableStorage)
        {
            float amountNeeded = config.idealGramAmount - currentPortafilterAmount;
            float amountToDispense = UnityEngine.Mathf.Min(amountNeeded, availableStorage);
            
            bool willReachIdeal = (currentPortafilterAmount + amountToDispense) >= config.idealGramAmount;
            
            return new DosingCalculation
            {
                AmountToDispense = amountToDispense,
                AmountNeeded = amountNeeded,
                WillReachIdeal = willReachIdeal,
                ResultingAmount = currentPortafilterAmount + amountToDispense
            };
        }

        /// <summary>
        /// Get processing time based on upgrade level
        /// </summary>
        public float GetProcessingTime(int upgradeLevel)
        {
            return upgradeLevel switch
            {
                0 => 0f, // Manual, instant
                1 => config.level1AutoDoseTime,
                2 => config.level1AutoDoseTime, // Same as level 1
                _ => config.level1AutoDoseTime
            };
        }

        #endregion

        #region Quality Evaluation

        /// <summary>
        /// Evaluate coffee quality based on amount
        /// </summary>
        public QualityResult EvaluateQuality(float coffeeAmount)
        {
            float qualityScore = qualityEvaluator.EvaluateQuality(coffeeAmount);
            var qualityLevel = qualityEvaluator.GetQualityLevel(qualityScore);
            string description = qualityEvaluator.GetQualityDescription(qualityScore);

            return new QualityResult
            {
                Score = qualityScore,
                Level = qualityLevel,
                Description = description,
                Amount = coffeeAmount
            };
        }

        #endregion

        #region Upgrade Logic

        /// <summary>
        /// Check if operation is valid for current upgrade level
        /// </summary>
        public bool CanPerformOperation(int upgradeLevel, DosingOperation operation)
        {
            return operation switch
            {
                DosingOperation.ManualHold => upgradeLevel == 0,
                DosingOperation.ButtonPress => upgradeLevel == 1,
                DosingOperation.AutoDose => upgradeLevel == 2,
                _ => false
            };
        }

        /// <summary>
        /// Get interaction type for upgrade level
        /// </summary>
        public InteractionType GetInteractionType(int upgradeLevel)
        {
            return upgradeLevel switch
            {
                0 => InteractionType.ManualLever,
                1 => InteractionType.ButtonPress,
                2 => InteractionType.AutoProcess,
                _ => InteractionType.ManualLever
            };
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Result of dosing calculations
    /// </summary>
    public struct DosingCalculation
    {
        public float AmountToDispense;
        public float AmountNeeded;
        public bool WillReachIdeal;
        public float ResultingAmount;
    }

    /// <summary>
    /// Result of quality evaluation
    /// </summary>
    public struct QualityResult
    {
        public float Score;
        public CoffeeQualityEvaluator.QualityLevel Level;
        public string Description;
        public float Amount;
    }

    /// <summary>
    /// Types of dosing operations
    /// </summary>
    public enum DosingOperation
    {
        ManualHold,
        ButtonPress,
        AutoDose
    }

    #endregion
}
