using System;
using UnityEngine;
using ProjectCoffee.Services.Interfaces;
using ProjectCoffee.Core.Services;

namespace ProjectCoffee.Services
{
    /// <summary>
    /// Service for managing coffee gramming machine state and logic
    /// </summary>
    public class CoffeeGrammingService : MachineService, IGrammingService
    {
        // Events specific to gramming machine
        public event Action<float> OnCoffeeAmountChanged;
        public event Action<float> OnPortafilterFillChanged;
        public event Action<CoffeeQualityEvaluator.QualityLevel> OnQualityEvaluated;
        
        private float storedCoffeeAmount = 0f;
        private bool hasPortafilter = false;
        private float portafilterCoffeeAmount = 0f;
        private CoffeeQualityEvaluator qualityEvaluator;
        private bool isDispensing = false;
        
        public float StoredCoffeeAmount => storedCoffeeAmount;
        public bool HasPortafilter => hasPortafilter;
        public float PortafilterCoffeeAmount => portafilterCoffeeAmount;
        public float MaxStorageCapacity => config != null ? ((GrammingMachineConfig)config).maxStorageCapacity : 100f;
        
        public CoffeeGrammingService(GrammingMachineConfig config) : base(config)
        {
            qualityEvaluator = new CoffeeQualityEvaluator(
                config != null ? config.idealGramAmount : 18f,
                config != null ? config.gramTolerance : 1f
            );
        }
        
        /// <summary>
        /// Add coffee to storage
        /// </summary>
        public bool AddCoffee(float amount)
        {
            if (amount <= 0)
                return false;
                
            float maxCapacity = ((GrammingMachineConfig)config).maxStorageCapacity;
            
            if (storedCoffeeAmount + amount > maxCapacity)
            {
                float actualAmount = maxCapacity - storedCoffeeAmount;
                storedCoffeeAmount = maxCapacity;
                OnCoffeeAmountChanged?.Invoke(storedCoffeeAmount);
                NotifyUser($"Added {actualAmount:F1}g to storage. Storage full!");
                UpdateState();
                return false;
            }
            
            storedCoffeeAmount += amount;
            OnCoffeeAmountChanged?.Invoke(storedCoffeeAmount);
            NotifyUser($"Added {amount:F1}g to storage. Total: {storedCoffeeAmount:F1}g");
            UpdateState();
            return true;
        }
        
        /// <summary>
        /// Set portafilter presence
        /// </summary>
        public void SetPortafilterPresent(bool present)
        {
            hasPortafilter = present;
            
            if (!present)
            {
                portafilterCoffeeAmount = 0f;
                OnPortafilterFillChanged?.Invoke(0f);
            }
            
            UpdateState();
        }
        
        /// <summary>
        /// Handle dispensing button hold
        /// </summary>
        public void OnDispensingHold(float deltaTime)
        {
            if (!hasPortafilter || storedCoffeeAmount <= 0)
            {
                Debug.Log($"Cannot dispense: hasPortafilter={hasPortafilter}, storedCoffeeAmount={storedCoffeeAmount}");
                return;
            }
            
            if (!isDispensing)
            {
                isDispensing = true;
                TransitionTo(MachineState.Processing);
            }
            
            float rate = ((GrammingMachineConfig)config).grammingRate;
            float amountToDispense = rate * deltaTime;
            amountToDispense = Mathf.Min(amountToDispense, storedCoffeeAmount);
            
            Debug.Log($"Dispensing: rate={rate}g/s, deltaTime={deltaTime}s, amountToDispense={amountToDispense}g, currentPortafilterAmount={portafilterCoffeeAmount}g");
            
            if (amountToDispense > 0)
            {
                storedCoffeeAmount -= amountToDispense;
                portafilterCoffeeAmount += amountToDispense;
                
                OnCoffeeAmountChanged?.Invoke(storedCoffeeAmount);
                OnPortafilterFillChanged?.Invoke(portafilterCoffeeAmount);
                
                if (storedCoffeeAmount <= 0)
                {
                    NotifyUser("Out of ground coffee!");
                }
            }
        }
        
        /// <summary>
        /// Handle dispensing button release
        /// </summary>
        public void OnDispensingRelease()
        {
            if (!isDispensing)
                return;
                
            isDispensing = false;
            
            // Evaluate quality
            float quality = qualityEvaluator.EvaluateQuality(portafilterCoffeeAmount);
            CoffeeQualityEvaluator.QualityLevel qualityLevel = qualityEvaluator.GetQualityLevel(quality);
            OnQualityEvaluated?.Invoke(qualityLevel);
            
            string qualityDesc = qualityEvaluator.GetQualityDescription(quality);
            if (portafilterCoffeeAmount > 0)
            {
                NotifyUser($"{qualityDesc} coffee weight: {portafilterCoffeeAmount:F1}g");
            }
            
            CompleteProcessing();
            UpdateState();
        }
        
        /// <summary>
        /// Clear portafilter
        /// </summary>
        public void ClearPortafilter()
        {
            portafilterCoffeeAmount = 0f;
            OnPortafilterFillChanged?.Invoke(0f);
            UpdateState();
        }
        
        /// <summary>
        /// Check for automatic operation
        /// </summary>
        public void CheckAutoOperation()
        {
            if (upgradeLevel >= 2 && hasPortafilter && storedCoffeeAmount > 0 && 
                portafilterCoffeeAmount == 0 && currentState == MachineState.Ready)
            {
                // Auto-dose perfect amount
                float idealAmount = ((GrammingMachineConfig)config).idealGramAmount;
                float amountToDispense = Mathf.Min(idealAmount, storedCoffeeAmount);
                
                storedCoffeeAmount -= amountToDispense;
                portafilterCoffeeAmount = amountToDispense;
                
                OnCoffeeAmountChanged?.Invoke(storedCoffeeAmount);
                OnPortafilterFillChanged?.Invoke(portafilterCoffeeAmount);
                
                // Evaluate quality
                float quality = qualityEvaluator.EvaluateQuality(portafilterCoffeeAmount);
                CoffeeQualityEvaluator.QualityLevel qualityLevel = qualityEvaluator.GetQualityLevel(quality);
                OnQualityEvaluated?.Invoke(qualityLevel);
                
                NotifyUser($"Auto-dosed {amountToDispense:F1}g of coffee");
                UpdateState();
            }
        }
        
        /// <summary>
        /// Update machine state based on current conditions
        /// </summary>
        private void UpdateState()
        {
            if (hasPortafilter && storedCoffeeAmount > 0 && (currentState == MachineState.Idle || currentState == MachineState.Processing))
            {
                if (!isDispensing)
                {
                    TransitionTo(MachineState.Ready);
                }
            }
            else if ((!hasPortafilter || storedCoffeeAmount <= 0) && isDispensing)
            {
                isDispensing = false;
                TransitionTo(MachineState.Idle);
            }
            else if (!hasPortafilter && currentState != MachineState.Idle)
            {
                TransitionTo(MachineState.Idle);
            }
            else if (portafilterCoffeeAmount > 0 && !isDispensing && currentState != MachineState.Complete)
            {
                TransitionTo(MachineState.Complete);
            }
        }
        
        protected override void OnUpgradeLevelChanged(int level)
        {
            switch (level)
            {
                case 0:
                    // Manual hold operation
                    NotifyUser("Manual gramming machine - hold button to dispense");
                    break;
                case 1:
                    // Single button press for perfect dose
                    NotifyUser("Semi-auto gramming - press once for 18g dose");
                    break;
                case 2:
                    // Automatic dosing when portafilter detected
                    NotifyUser("Auto gramming - automatically doses when portafilter placed");
                    break;
            }
        }
    }
}
