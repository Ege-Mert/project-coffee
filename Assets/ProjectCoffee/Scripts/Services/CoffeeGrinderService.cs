using System;
using UnityEngine;
using ProjectCoffee.Services.Interfaces;
using ProjectCoffee.Core.Services;

namespace ProjectCoffee.Services
{
    /// <summary>
    /// Service for managing coffee grinder state and logic
    /// </summary>
    public class CoffeeGrinderService : MachineService, IGrinderService
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
        /// Called when ground coffee is created directly
        /// </summary>
        public void OnGroundCoffeeCreated()
        {
            Debug.Log("CoffeeGrinderService: Ground coffee was created directly");
            
            // Consume a bean
            if (currentBeanFills > 0)
            {
                currentBeanFills--;
                OnBeanCountChanged?.Invoke(currentBeanFills);
            }
            
            // Set the state
            hasExistingGroundCoffee = true;
            currentCoffeeSize = GroundCoffee.GrindSize.Small;
        }
        
        /// <summary>
        /// Called when ground coffee size changes directly
        /// </summary>
        public void OnGroundCoffeeSizeChanged(GroundCoffee.GrindSize newSize)
        {
            Debug.Log($"CoffeeGrinderService: Ground coffee size changed to {newSize}");
            currentCoffeeSize = newSize;
            hasExistingGroundCoffee = true;
            
            // Consume a bean when upgrading coffee
            if (currentBeanFills > 0)
            {
                currentBeanFills--;
                OnBeanCountChanged?.Invoke(currentBeanFills);
            }
        }
        
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
            // Early exit if already processing to avoid log spam
            if (isProcessingGrinding)
                return;
                
            Debug.Log("CoffeeGrinderService: OnButtonPressed called");
            
            if (upgradeLevel < 1 || !HasBeans)
            {
                Debug.LogWarning($"CoffeeGrinderService: Cannot process - upgradeLevel: {upgradeLevel}, HasBeans: {HasBeans}");
                return;
            }
            
            StartProcessing();
        }
        
        public override void StartProcessing()
        {
            Debug.Log("CoffeeGrinderService: StartProcessing called");
            
            // First make sure we're not already processing
            if (isProcessingGrinding)
            {
                Debug.LogWarning("CoffeeGrinderService: Already processing, can't start new process");
                return;
            }
            
            if (!CanProcess())
            {
                Debug.LogWarning("CoffeeGrinderService: Cannot start processing - CanProcess() returned false");
                return;
            }
            
            base.StartProcessing();
            isProcessingGrinding = true;
            
            // Level 1+ uses timed processing
            if (upgradeLevel >= 1)
            {
                float processTime = ((GrinderConfig)config).level1GrindTime;
                
                // For level 2, use a shorter processing time but not instant
                if (upgradeLevel >= 2)
                {
                    processTime = ((GrinderConfig)config).level2GrindTime;
                }
                
                Debug.Log($"CoffeeGrinderService: Started timed processing, duration: {processTime} seconds");
                // The MonoBehaviour will handle the timing and call ProcessUpdate
                NotifyUser("Grinding coffee...");
                
                // Instead of immediately completing, just set the progress
                // so it will finish after a short visual feedback period
                if (upgradeLevel >= 2)
                {
                    // We'll let the normal ProcessUpdate handle the completion
                    // but start with 50% progress so it's faster
                    processProgress = 0.5f;
                    Debug.Log("CoffeeGrinderService: Auto-grinding at level 2 (not instant but faster)");
                }
            }
        }
        
        /// <summary>
        /// Force complete the processing (used as a failsafe)
        /// </summary>
        public void ForceCompleteProcessing()
        {
            Debug.LogWarning("CoffeeGrinderService: Forcing completion of processing");
            
            if (currentState != MachineState.Processing)
            {
                Debug.LogWarning("CoffeeGrinderService: Cannot force complete - not in Processing state");
                return;
            }
            
            // Force progress to 100%
            processProgress = 1.0f;
            UpdateProgress(processProgress);
            
            // Process the grinding
            ProcessGrinding();
        }
        
        /// <summary>
        /// Update processing progress (called by MonoBehaviour)
        /// </summary>
        public void ProcessUpdate(float deltaTime)
        {
            if (currentState != MachineState.Processing)
            {
                Debug.LogWarning($"CoffeeGrinderService: ProcessUpdate called but state is {currentState}, not Processing");
                return;
            }
            
            // Get the appropriate process time based on upgrade level
            float processTime = upgradeLevel >= 2 ? 
                ((GrinderConfig)config).level2GrindTime : 
                ((GrinderConfig)config).level1GrindTime;
                
            float oldProgress = processProgress;
            processProgress += deltaTime / processTime;
            UpdateProgress(processProgress);
            
            // Log progress changes
            if (Mathf.FloorToInt(processProgress * 10) > Mathf.FloorToInt(oldProgress * 10))
            {
                Debug.Log($"CoffeeGrinderService: Process progress updated to {processProgress:P0}");
            }
            
            if (processProgress >= 1.0f)
            {
                Debug.Log("CoffeeGrinderService: Process completed via ProcessUpdate");
                // Now directly call ProcessGrinding to create/upgrade the coffee
                isProcessingGrinding = false; // Reset flag before calling ProcessGrinding
                ProcessGrinding();
            }
        }
        
        /// <summary>
        /// Process the grinding operation
        /// </summary>
        private void ProcessGrinding()
        {
            // Add debug log to track this method being called
            Debug.Log($"CoffeeGrinderService: ProcessGrinding called with beans: {HasBeans}, hasExistingGroundCoffee: {hasExistingGroundCoffee}");
            
            // Early exit for no beans case
            if (!HasBeans)
            {
                Debug.Log("CoffeeGrinderService: No beans to process");
                isProcessingGrinding = false;
                CompleteProcessing();
                UpdateState();
                return;
            }
            
            // CRITICAL FIX: Since we're directly calling ProcessGrinding at level 2, we should NOT
            // exit early if isProcessingGrinding is true. Instead, we reset the flag here.
            if (isProcessingGrinding && upgradeLevel < 2)
            {
                Debug.Log("CoffeeGrinderService: Already processing grinding, waiting to complete");
                return;
            }
            
            // Force reset the processing flag to avoid getting stuck
            isProcessingGrinding = true;
            
            try
            {
                // If no existing ground coffee, create a new small chunk
                if (!hasExistingGroundCoffee)
                {
                    currentBeanFills--;
                    OnBeanCountChanged?.Invoke(currentBeanFills);
                    
                    currentCoffeeSize = GroundCoffee.GrindSize.Small;
                    hasExistingGroundCoffee = true;
                    Debug.Log("CoffeeGrinderService: Creating NEW ground coffee at size Small");
                    OnCoffeeOutputReady?.Invoke(currentCoffeeSize);
                    spinsSinceLastBeanConsumption = 0;
                    Debug.Log("CoffeeGrinderService: Coffee output event fired, beans remaining: " + currentBeanFills);
                }
                else
                {
                    // Upgrade existing coffee chunk
                    if (currentCoffeeSize == GroundCoffee.GrindSize.Small)
                    {
                        currentBeanFills--;
                        OnBeanCountChanged?.Invoke(currentBeanFills);
                        
                        currentCoffeeSize = GroundCoffee.GrindSize.Medium;
                        Debug.Log("CoffeeGrinderService: UPGRADING ground coffee to Medium");
                        OnCoffeeSizeUpgraded?.Invoke(currentCoffeeSize);
                        spinsSinceLastBeanConsumption = 0;
                        Debug.Log("CoffeeGrinderService: Coffee upgrade event fired, beans remaining: " + currentBeanFills);
                    }
                    else if (currentCoffeeSize == GroundCoffee.GrindSize.Medium)
                    {
                        currentBeanFills--;
                        OnBeanCountChanged?.Invoke(currentBeanFills);
                        
                        currentCoffeeSize = GroundCoffee.GrindSize.Large;
                        Debug.Log("CoffeeGrinderService: UPGRADING ground coffee to Large");
                        OnCoffeeSizeUpgraded?.Invoke(currentCoffeeSize);
                        spinsSinceLastBeanConsumption = 0;
                        Debug.Log("CoffeeGrinderService: Coffee upgrade event fired, beans remaining: " + currentBeanFills);
                    }
                    // If already large, don't consume more beans but reset spin count
                    else
                    {
                        spinsSinceLastBeanConsumption = 0;
                        Debug.Log("CoffeeGrinderService: Coffee already at maximum size");
                        NotifyUser("Ground coffee is already at maximum size!");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("CoffeeGrinderService: Error during ProcessGrinding: " + e.Message);
            }
            finally
            {
                // IMPORTANT: Always reset the processing flag
                isProcessingGrinding = false;
                CompleteProcessing();
                UpdateState();
                
                Debug.Log("CoffeeGrinderService: Processing complete, hasExistingGroundCoffee: " + hasExistingGroundCoffee);
            }
        }
        
        /// <summary>
        /// Called when ground coffee is removed from the output zone
        /// </summary>
        public void OnGroundCoffeeRemoved()
        {
            Debug.Log("CoffeeGrinderService: Ground coffee was removed");
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

        /// <summary>
        /// Force the state to Ready to ensure auto-processing can start
        /// </summary>
        public void ForceReadyState()
        {
            if (currentState != MachineState.Ready && HasBeans)
            {
                Debug.Log("CoffeeGrinderService: Forcing state to Ready");
                TransitionTo(MachineState.Ready);
            }
        }
        
        protected override void OnUpgradeLevelChanged(int level)
        {
            // Handle upgrade-specific changes
            switch (level)
            {
                case 0:
                    // Manual lever operation
                    NotifyUser("Basic grinder: Use the lever to grind beans.");
                    break;
                case 1:
                    // Button press operation
                    NotifyUser("Grinder upgraded to level 1! Now uses button press.");
                    break;
                case 2:
                    // Fully automatic operation - all beans at once
                    NotifyUser("Grinder upgraded to level 2! Now automatically grinds all beans.");
                    break;
            }
        }
        
        /// <summary>
        /// Check for automatic operation (Level 2)
        /// </summary>
        public void CheckAutoProcess()
        {
            // For level 2 (Automatic Grinder), always process beans if available
            if (upgradeLevel >= 2 && HasBeans && !isProcessingGrinding && currentState == MachineState.Ready)
            {
                Debug.Log("CoffeeGrinderService: Auto-processing triggered at level 2");
                
                // Even if there's already ground coffee, we should process more beans
                // as long as it's not already at the largest size
                if (!hasExistingGroundCoffee || currentCoffeeSize != GroundCoffee.GrindSize.Large)
                {
                    // Always process beans automatically for level 2
                    StartProcessing();
                }
                else
                {
                    Debug.Log("CoffeeGrinderService: Cannot auto-process - coffee is already at maximum size");
                }
            }
        }
    }
}