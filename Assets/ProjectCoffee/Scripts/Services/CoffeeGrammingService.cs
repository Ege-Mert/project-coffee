using System;
using UnityEngine;
using ProjectCoffee.Services.Interfaces;

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
        public event Action OnAutoDoseStarted;
        public event Action OnAutoDoseCompleted;
        
        private float storedCoffeeAmount = 0f;
        private bool hasPortafilter = false;
        private float portafilterCoffeeAmount = 0f;
        private CoffeeQualityEvaluator qualityEvaluator;
        private bool isDispensing = false;
        
        // Flag to track if we need to check auto-dosing conditions
        private bool pendingAutoDosingCheck = false;
        
        public float StoredCoffeeAmount => storedCoffeeAmount;
        public bool HasPortafilter => hasPortafilter;
        public float PortafilterCoffeeAmount => portafilterCoffeeAmount;
        public float MaxStorageCapacity => config != null ? ((GrammingMachineConfig)config).maxStorageCapacity : 100f;
        
        /// <summary>
        /// Public method to force transition to a specific state
        /// Used by the machine for direct control in special cases
        /// </summary>
        public void TransitionToState(MachineState newState)
        {
            Debug.Log($"TransitionToState: Force transitioning from {currentState} to {newState}");
            TransitionTo(newState);
        }
        
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
        
        Debug.Log($"AddCoffee called with {amount}g, current storage: {storedCoffeeAmount}g, hasPortafilter: {hasPortafilter}, level: {upgradeLevel}");
        
        float maxCapacity = ((GrammingMachineConfig)config).maxStorageCapacity;
        
        if (storedCoffeeAmount + amount > maxCapacity)
        {
        float actualAmount = maxCapacity - storedCoffeeAmount;
        storedCoffeeAmount = maxCapacity;
        OnCoffeeAmountChanged?.Invoke(storedCoffeeAmount);
            NotifyUser($"Added {actualAmount:F1}g to storage. Storage full!");
            UpdateState();
            
            // Check for auto-operation at level 2 when coffee is added
            if (upgradeLevel == 2 && hasPortafilter)
        {
                Debug.Log("Coffee added and portafilter present at level 2, checking auto-operation");
                
            // Force state to Ready if not already
                if (currentState != MachineState.Ready)
                {
                    Debug.Log($"Forcing state from {currentState} to Ready");
                    TransitionTo(MachineState.Ready);
                }
                
                // Schedule auto-operation with a slight delay to ensure UI updates
                UnityEngine.MonoBehaviour.FindObjectOfType<UnityEngine.MonoBehaviour>()?.StartCoroutine(
                    DelayedAutoOperationCheck());
            }
        
        return false;
        }
        
        storedCoffeeAmount += amount;
            OnCoffeeAmountChanged?.Invoke(storedCoffeeAmount);
        NotifyUser($"Added {amount:F1}g to storage. Total: {storedCoffeeAmount:F1}g");
        UpdateState();
        
        // Check for auto-operation at level 2 when coffee is added
        if (upgradeLevel == 2 && hasPortafilter)
        {
            Debug.Log("Coffee added and portafilter present at level 2, checking auto-operation");
            
            // Force state to Ready if not already
            if (currentState != MachineState.Ready)
            {
                Debug.Log($"Forcing state from {currentState} to Ready");
                TransitionTo(MachineState.Ready);
            }
            
            // Schedule auto-operation with a slight delay to ensure UI updates
            UnityEngine.MonoBehaviour.FindObjectOfType<UnityEngine.MonoBehaviour>()?.StartCoroutine(
                DelayedAutoOperationCheck());
        }
        
        return true;
    }
        
        /// <summary>
        /// Set portafilter presence
        /// </summary>
        public void SetPortafilterPresent(bool present)
        {
            Debug.Log($"SetPortafilterPresent called with present={present}, previous hasPortafilter={hasPortafilter}");
            bool previousHadPortafilter = hasPortafilter;
            hasPortafilter = present;
            
            if (!present)
            {
                portafilterCoffeeAmount = 0f;
                OnPortafilterFillChanged?.Invoke(0f);
                Debug.Log("Portafilter removed - reset portafilterCoffeeAmount to 0");
            }
            else if (!previousHadPortafilter && present) 
            {
                // New portafilter just arrived
                Debug.Log("New portafilter detected");
                
                // If at level 2 and we have coffee in storage, schedule an auto-dose check
                if (upgradeLevel == 2 && storedCoffeeAmount > 0)
                {
                    Debug.Log("Level 2: New portafilter + coffee in storage = scheduling auto-dose check");
                    
                    // Force state to Ready if needed
                    if (currentState == MachineState.Idle)
                    {
                        Debug.Log("Forcing state from Idle to Ready");
                        TransitionTo(MachineState.Ready);
                    }
                    
                    // Schedule auto operation check with slight delay to ensure UI updates
                    UnityEngine.MonoBehaviour.FindObjectOfType<UnityEngine.MonoBehaviour>()?.StartCoroutine(
                        DelayedAutoOperationCheck());
                }
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
        /// Update process progress (available to MonoBehaviour for animation)
        /// </summary>
        public new void UpdateProgress(float progress)
        {
            // Call the protected base method to handle the event invocation
            base.UpdateProgress(progress);
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
        /// Check for automatic operation (Level 2)
        /// </summary>
        public void CheckAutoOperation()
        {
            // Only check for auto-operation at level 2
            if (upgradeLevel != 2)
            {
                Debug.Log($"CheckAutoOperation: Not level 2 (current level: {upgradeLevel})");
                return;
            }
            
            if (!hasPortafilter)
            {
                Debug.Log("CheckAutoOperation: No portafilter present");
                return;
            }
            
            if (storedCoffeeAmount <= 0)
            {
                Debug.Log("CheckAutoOperation: No coffee in storage");
                return;
            }
            
            if (currentState != MachineState.Ready && currentState != MachineState.Idle)
            {
                Debug.Log($"CheckAutoOperation: Machine not in ready state (current state: {currentState})");
                return;
            }
            
            // Get the ideal amount and see if we need to top-off the portafilter
            float idealAmount = ((GrammingMachineConfig)config).idealGramAmount;
            float amountNeeded = idealAmount - portafilterCoffeeAmount;
            
            Debug.Log($"CheckAutoOperation: portafilterCoffeeAmount={portafilterCoffeeAmount}, idealAmount={idealAmount}, amountNeeded={amountNeeded}");
            
            if (amountNeeded > 0)
            {
                Debug.Log($"CheckAutoOperation: Starting automatic dosing (Level 2) - Portafilter has {portafilterCoffeeAmount}g, needs {amountNeeded}g more");
                
                // Start processing visual feedback
                StartProcessing();
                
                // Notify that auto-dosing started
                OnAutoDoseStarted?.Invoke();
                
                // Use a coroutine-like approach to add a slight delay
                // This will be scheduled on the next frame
                UnityEngine.MonoBehaviour.FindObjectOfType<UnityEngine.MonoBehaviour>()?.StartCoroutine(
                    PerformDelayedAutoDose());
            }
            else if (portafilterCoffeeAmount >= idealAmount)
            {
                Debug.Log($"CheckAutoOperation: Portafilter already has {portafilterCoffeeAmount}g, not starting auto-dose");
            }
        }
        
        /// <summary>
        /// Perform auto-dosing with configurable delays for visual feedback
        /// </summary>
        private System.Collections.IEnumerator PerformDelayedAutoDose()
        {
            // Get detection delay from config
            float autoDetectionDelay = 0.5f;
            float autoProcessingTime = 2.0f;
            
            if (config is GrammingMachineConfig grammingConfig)
            {
                autoDetectionDelay = grammingConfig.level2AutoDetectionInterval;
                autoProcessingTime = grammingConfig.level1AutoDoseTime; // Use the same processing time as level 1
                Debug.Log($"Using config delays: detection={autoDetectionDelay}s, processing={autoProcessingTime}s");
            }
            
            // Add initial detection delay
            yield return new UnityEngine.WaitForSeconds(autoDetectionDelay);
            
            // Now start the processing animation
            Debug.Log("Starting auto-dose processing animation");
            
            // Animate the processing over time
            float startTime = UnityEngine.Time.time;
            while (UnityEngine.Time.time < startTime + autoProcessingTime)
            {
                // Update progress for visuals
                float progress = (UnityEngine.Time.time - startTime) / autoProcessingTime;
                UpdateProgress(progress);
                yield return null;
            }
            
            // Ensure progress is at 100%
            UpdateProgress(1.0f);
            
            // Now perform the actual dosing
            PerformAutoDose();
            
            // Complete the processing
            CompleteProcessing();
            
            // Notify that auto-dosing completed
            OnAutoDoseCompleted?.Invoke();
        }
        
        /// <summary>
        /// Perform auto-dosing to an ideal amount (used by both Level 1 and Level 2)
        /// </summary>
        public void PerformAutoDose()
        {
            if (!hasPortafilter || storedCoffeeAmount <= 0)
            {
                Debug.LogWarning("Cannot perform auto-dose: Invalid conditions");
                return;
            }
            
            // Calculate the perfect amount from config
            float idealAmount = ((GrammingMachineConfig)config).idealGramAmount;
            
            // Calculate how much coffee we need to add to reach the ideal amount
            float amountNeeded = idealAmount - portafilterCoffeeAmount;
            
            Debug.Log($"CoffeeGrammingService: Portafilter has {portafilterCoffeeAmount}g, ideal is {idealAmount}g, need to add {amountNeeded}g");
            
            // If portafilter already has enough coffee, no need to add more
            if (amountNeeded <= 0)
            {
            Debug.Log("Portafilter already has enough coffee, no need to add more");
            NotifyUser("Portafilter already has enough coffee!");
            CompleteProcessing(); // Make sure to complete the processing state
                return;
            }
            
            // Limit the amount to dispense based on available coffee in storage
            float amountToDispense = Mathf.Min(amountNeeded, storedCoffeeAmount);
            
            // Special case for level 1 - if storage has less than ideal and portafilter has coffee,
            // clear portafilter first and use all available coffee for a fresh dose
            if (upgradeLevel == 1 && portafilterCoffeeAmount > 0 && storedCoffeeAmount < amountNeeded)
            {
                Debug.Log($"Level 1 special case: Clearing portafilter that had {portafilterCoffeeAmount}g and using all {storedCoffeeAmount}g from storage for fresh dose");
                portafilterCoffeeAmount = 0;
                amountToDispense = storedCoffeeAmount;
                amountNeeded = idealAmount; // Reset the needed amount
            }
            
            Debug.Log($"CoffeeGrammingService: Auto-dosing {amountToDispense}g of coffee (needed: {amountNeeded}g)");
            
            // Update coffee amounts - add to existing amount rather than replacing
            storedCoffeeAmount -= amountToDispense;
            portafilterCoffeeAmount += amountToDispense;
            
            // Notify listeners
            OnCoffeeAmountChanged?.Invoke(storedCoffeeAmount);
            OnPortafilterFillChanged?.Invoke(portafilterCoffeeAmount);
            
        // Evaluate quality
            float quality = qualityEvaluator.EvaluateQuality(portafilterCoffeeAmount);
            CoffeeQualityEvaluator.QualityLevel qualityLevel = qualityEvaluator.GetQualityLevel(quality);
            OnQualityEvaluated?.Invoke(qualityLevel);
        
            // Notify user
            string qualityDesc = qualityEvaluator.GetQualityDescription(quality);
            
            // Different message depending on the level and situation
                if (upgradeLevel == 1)
            {
                // For level 1, always report the auto-dose amount
                NotifyUser($"Auto-dosed to {portafilterCoffeeAmount:F1}g - {qualityDesc}!");
            }
            else if (amountToDispense < amountNeeded)
            {
                // For level 2, report that we added some coffee but couldn't reach ideal
                NotifyUser($"Added {amountToDispense:F1}g of coffee. Portafilter now has {portafilterCoffeeAmount:F1}g - {qualityDesc}!");
            }
            else
            {
                // For level 2, report that we topped off to perfect
                NotifyUser($"Automatically dosed to perfect {portafilterCoffeeAmount:F1}g - {qualityDesc}!");
            }
            
            // Update state
            UpdateState();
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
                    NotifyUser("Manual gramming machine - Hold button to dispense coffee");
                    break;
                case 1:
                    // Single button press for perfect dose
                    NotifyUser("Upgraded to Semi-Auto gramming - Press button once for perfect 18g dose");
                    break;
                case 2:
                    // Automatic dosing when portafilter detected
                    NotifyUser("Upgraded to Fully Automatic gramming - Automatically doses when portafilter is placed");
                    
                    // If we already have a portafilter, check if we need to auto-dose
                    if (hasPortafilter && storedCoffeeAmount > 0 && portafilterCoffeeAmount < ((GrammingMachineConfig)config).idealGramAmount)
                    {
                        // Schedule an auto-operation check after a short delay to allow UI to update
                        UnityEngine.MonoBehaviour.FindObjectOfType<UnityEngine.MonoBehaviour>()?.StartCoroutine(
                            DelayedAutoOperationCheck());
                    }
                    break;
            }
        }
        
        /// <summary>
        /// Check for auto-operation after a short delay
        /// </summary>
        private System.Collections.IEnumerator DelayedAutoOperationCheck()
        {
            Debug.Log("DelayedAutoOperationCheck started");
            yield return new UnityEngine.WaitForSeconds(0.3f);
            
            // Double-check our state conditions
            if (upgradeLevel != 2 || !hasPortafilter || storedCoffeeAmount <= 0)
            {
                Debug.Log($"DelayedAutoOperationCheck: Conditions not met - level: {upgradeLevel}, hasPortafilter: {hasPortafilter}, storedCoffeeAmount: {storedCoffeeAmount}");
                yield break;
            }
            
            // Force state to Ready if needed
            if (currentState != MachineState.Ready)
            {
                Debug.Log($"DelayedAutoOperationCheck: Forcing state from {currentState} to Ready");
                TransitionTo(MachineState.Ready);
            }
            
            // Get the ideal amount and see if we need to top-off the portafilter
            float idealAmount = ((GrammingMachineConfig)config).idealGramAmount;
            float amountNeeded = idealAmount - portafilterCoffeeAmount;
            
            Debug.Log($"DelayedAutoOperationCheck: portafilterCoffeeAmount={portafilterCoffeeAmount}, idealAmount={idealAmount}, amountNeeded={amountNeeded}");
            
            if (amountNeeded > 0)
            {
                Debug.Log($"DelayedAutoOperationCheck: Starting automatic dosing - Portafilter needs {amountNeeded}g more");
                
                // Directly start the auto-dosing process
                // Start processing visual feedback
                StartProcessing();
                
                // Notify that auto-dosing started
                OnAutoDoseStarted?.Invoke();
                
                // Start the auto-dosing animation
                yield return PerformDelayedAutoDose();
            }
            else
            {
                Debug.Log($"DelayedAutoOperationCheck: Portafilter already has {portafilterCoffeeAmount}g, no need to auto-dose");
            }
        }
        
        /// <summary>
        /// Request an auto-dosing check on the next update cycle
        /// </summary>
        public void RequestAutoDosingCheck()
        {
            // Only set the flag if we're at level 2
            if (upgradeLevel == 2)
            {
                pendingAutoDosingCheck = true;
            }
        }

        /// <summary>
        /// Process any pending auto-dosing checks - called from the machine's LateUpdate
        /// </summary>
        public void ProcessPendingChecks()
        {
            // Only process if we have a pending check and we're at level 2
            if (!pendingAutoDosingCheck || upgradeLevel != 2)
            {
                return;
            }
            
            // Clear the flag immediately to prevent duplicate processing
            pendingAutoDosingCheck = false;
            
            // Only proceed if we have both requirements
            if (hasPortafilter && storedCoffeeAmount > 0)
            {
                // Don't start auto-dosing if we're already processing
                if (currentState == MachineState.Processing)
                {
                    Debug.Log("ProcessPendingChecks: Already processing, skipping auto-dosing check");
                    return;
                }
                
                // Force to ready state if needed
                if (currentState != MachineState.Ready)
                {
                    Debug.Log("ProcessPendingChecks: Setting state to Ready for auto-dosing");
                    TransitionTo(MachineState.Ready);
                }
                
                // Now check for auto-operation
                CheckAutoOperation();
            }
        }
    }
}
