using System;
using UnityEngine;

namespace ProjectCoffee.Services
{
    /// <summary>
    /// Service for managing coffee grinder state and logic
    /// </summary>
    public class CoffeeGrinderService : MachineService
    {
        // Events specific to grinder
        public event Action<int> OnBeanCountChanged;
        public event Action<GroundCoffee.GrindSize> OnCoffeeOutputReady;
        public event Action<int> OnSpinCompleted;
        public event Action<GroundCoffee.GrindSize> OnCoffeeSizeUpgraded;
        public event Action OnGrindingStarted;
        
        private int currentBeanFills = 0;
        private int spinsSinceLastBeanConsumption = 0;
        private bool isProcessingGrinding = false;
        private bool hasExistingGroundCoffee = false;
        private GroundCoffee.GrindSize currentCoffeeSize = GroundCoffee.GrindSize.Small;
        
        public int CurrentBeanFills => currentBeanFills;
        public int MaxBeanFills => config != null ? ((GrinderConfig)config).maxBeanFills : 3;
        public bool CanAddBeans => currentBeanFills < MaxBeanFills;
        public bool HasBeans => currentBeanFills > 0;
        public bool HasExistingGroundCoffee => hasExistingGroundCoffee;
        public GroundCoffee.GrindSize CurrentCoffeeSize => currentCoffeeSize;
        
        public CoffeeGrinderService(GrinderConfig config) : base(config) { }
        
        /// <summary>
        /// Add beans to the grinder
        /// </summary>
        public bool AddBeans(int amount)
        {
            if (!CanAddBeans)
            {
                NotifyUser("Grinder is full of beans!");
                return false;
            }
            
            int previousAmount = currentBeanFills;
            currentBeanFills = Mathf.Min(currentBeanFills + amount, MaxBeanFills);
            
            if (currentBeanFills != previousAmount)
            {
                OnBeanCountChanged?.Invoke(currentBeanFills);
                NotifyUser($"Added beans. Total: {currentBeanFills}/{MaxBeanFills}");
                UpdateState();
            }
            
            return true;
        }
        
        /// <summary>
        /// Handle spin completion based on upgrade level
        /// </summary>
        public void OnHandleSpinCompleted()
        {
            if (!HasBeans || isProcessingGrinding)
                return;
                
            spinsSinceLastBeanConsumption++;
            OnSpinCompleted?.Invoke(spinsSinceLastBeanConsumption);
            
            int requiredSpins = GetRequiredSpins();
            
            // Always process when we've accumulated enough spins
            if (spinsSinceLastBeanConsumption >= requiredSpins)
            {
                ProcessGrinding();
            }
        }
        
        /// <summary>
        /// Handle button press (Level 1+)
        /// </summary>
        public void OnButtonPressed()
        {
            if (upgradeLevel < 1 || !HasBeans || isProcessingGrinding)
                return;
                
            StartProcessing();
        }
        
        public override void StartProcessing()
        {
            if (!CanProcess()) return;
            
            base.StartProcessing();
            isProcessingGrinding = true;
            
            // Level 1+ uses timed processing
            if (upgradeLevel >= 1)
            {
                float processTime = ((GrinderConfig)config).level1GrindTime;
                // The MonoBehaviour will handle the timing and call ProcessUpdate
                NotifyUser("Grinding coffee...");
            }
        }
        
        /// <summary>
        /// Update processing progress (called by MonoBehaviour)
        /// </summary>
        public void ProcessUpdate(float deltaTime)
        {
            if (currentState != MachineState.Processing) return;
            
            float processTime = ((GrinderConfig)config).level1GrindTime;
            processProgress += deltaTime / processTime;
            UpdateProgress(processProgress);
            
            if (processProgress >= 1f)
            {
                ProcessGrinding();
            }
        }
        
        /// <summary>
        /// Process the grinding operation
        /// </summary>
        private void ProcessGrinding()
        {
            if (!HasBeans || isProcessingGrinding)
                return;
                
            isProcessingGrinding = true;
            
            // If no existing ground coffee, create a new small chunk
            if (!hasExistingGroundCoffee)
            {
                currentBeanFills--;
                OnBeanCountChanged?.Invoke(currentBeanFills);
                
                currentCoffeeSize = GroundCoffee.GrindSize.Small;
                hasExistingGroundCoffee = true;
                OnCoffeeOutputReady?.Invoke(currentCoffeeSize);
                spinsSinceLastBeanConsumption = 0;
            }
            else
            {
                // Upgrade existing coffee chunk
                if (currentCoffeeSize == GroundCoffee.GrindSize.Small)
                {
                    currentBeanFills--;
                    OnBeanCountChanged?.Invoke(currentBeanFills);
                    
                    currentCoffeeSize = GroundCoffee.GrindSize.Medium;
                    OnCoffeeSizeUpgraded?.Invoke(currentCoffeeSize);
                    spinsSinceLastBeanConsumption = 0;
                }
                else if (currentCoffeeSize == GroundCoffee.GrindSize.Medium)
                {
                    currentBeanFills--;
                    OnBeanCountChanged?.Invoke(currentBeanFills);
                    
                    currentCoffeeSize = GroundCoffee.GrindSize.Large;
                    OnCoffeeSizeUpgraded?.Invoke(currentCoffeeSize);
                    spinsSinceLastBeanConsumption = 0;
                }
                // If already large, don't consume more beans but reset spin count
                else
                {
                    spinsSinceLastBeanConsumption = 0;
                    NotifyUser("Ground coffee is already at maximum size!");
                }
            }
            
            isProcessingGrinding = false;
            CompleteProcessing();
            UpdateState();
        }
        
        /// <summary>
        /// Called when ground coffee is removed from the output zone
        /// </summary>
        public void OnGroundCoffeeRemoved()
        {
            hasExistingGroundCoffee = false;
            currentCoffeeSize = GroundCoffee.GrindSize.Small;
            spinsSinceLastBeanConsumption = 0;
        }
        
        /// <summary>
        /// Determine ground coffee output size
        /// </summary>
        private GroundCoffee.GrindSize DetermineOutputSize()
        {
            // This logic can be expanded based on upgrade level
            // For now, always output small size
            return GroundCoffee.GrindSize.Small;
        }
        
        /// <summary>
        /// Get required spins based on upgrade level
        /// </summary>
        private int GetRequiredSpins()
        {
            if (upgradeLevel == 0)
            {
                return 1; // For progressive upgrading, each spin should trigger processing
            }
            return 1; // Level 1+ uses button/auto instead of spins
        }
        
        /// <summary>
        /// Update machine state based on current conditions
        /// </summary>
        private void UpdateState()
        {
            if (HasBeans && currentState == MachineState.Idle)
            {
                TransitionTo(MachineState.Ready);
            }
            else if (!HasBeans && currentState != MachineState.Idle)
            {
                TransitionTo(MachineState.Idle);
            }
        }
        
        protected override void OnUpgradeLevelChanged(int level)
        {
            // Handle upgrade-specific changes
            switch (level)
            {
                case 0:
                    // Manual lever operation
                    break;
                case 1:
                    // Button press operation
                    NotifyUser("Grinder upgraded! Now uses button press.");
                    break;
                case 2:
                    // Automatic operation
                    NotifyUser("Grinder upgraded! Now works automatically.");
                    break;
            }
        }
        
        /// <summary>
        /// Check for automatic operation (Level 2)
        /// </summary>
        public void CheckAutoProcess()
        {
            if (upgradeLevel >= 2 && HasBeans && !isProcessingGrinding && currentState == MachineState.Ready)
            {
                StartProcessing();
            }
        }
    }
}
