using System;
using UnityEngine;
using ProjectCoffee.Services.Interfaces;
using ProjectCoffee.Machines.Grinder.Logic;

namespace ProjectCoffee.Services.Grinder
{
    /// <summary>
    /// Simplified grinder service that coordinates between logic and state
    /// </summary>
    public class GrinderService : MachineService, IGrinderService
    {
        // IGrinderService Events
        public event Action<int> OnBeanCountChanged;
        public event Action<GroundCoffee.GrindSize> OnCoffeeOutputReady;
        public event Action<int> OnSpinCompleted;
        public event Action<GroundCoffee.GrindSize> OnCoffeeSizeUpgraded;
        public event Action OnGrindingStarted;
        
        private readonly GrinderLogic logic;
        private readonly GrinderState state;
        private float autoProcessTimer = 0f;
        private bool shouldContinueProcessing = false;
        private bool isWaitingForAutoProcess = false;
        
        #region IGrinderService Properties
        
        public int CurrentBeanFills => state.CurrentBeans;
        public int MaxBeanFills => ((GrinderConfig)config).maxBeanFills;
        public bool HasBeans => state.CurrentBeans > 0;
        public bool CanAddBeans => logic.CanAddBeans(state.CurrentBeans, 1);
        public bool HasExistingGroundCoffee => state.HasExistingCoffee;
        public GroundCoffee.GrindSize CurrentCoffeeSize => state.CurrentCoffeeSize;
        
        #endregion
        
        public GrinderService(GrinderConfig config) : base(config)
        {
            logic = new GrinderLogic(config);
            state = new GrinderState();
            
            SubscribeToStateEvents();
        }
        
        #region Event Subscription
        
        private void SubscribeToStateEvents()
        {
            state.OnBeanCountChanged += (count) => OnBeanCountChanged?.Invoke(count);
            state.OnStateChanged += HandleStateChanged;
            state.OnCoffeeCreated += (size) => OnCoffeeOutputReady?.Invoke(size);
            state.OnCoffeeUpgraded += (size) => OnCoffeeSizeUpgraded?.Invoke(size);
        }
        
        private void HandleStateChanged(MachineState machineState)
        {
            // Update the base class state
            TransitionTo(machineState);
        }
        
        #endregion
        
        #region IGrinderService Implementation
        
        public bool AddBeans(int amount)
        {
            var result = logic.AddBeans(state.CurrentBeans, amount);
            
            if (result.IsSuccess)
            {
                state.SetBeanCount(result.Data);
                NotifyUser(result.Message);
                
                Debug.Log($"GrinderService: {result.Message}");
                return true;
            }
            else
            {
                NotifyUser(result.Message);
                Debug.Log($"GrinderService: Failed to add beans - {result.Message}");
                return false;
            }
        }
        
        public void OnHandleSpinCompleted()
        {
            Debug.Log($"GrinderService: Handle spin completed. {state.GetStateInfo()}");
            
            if (!CanProcessSpin())
            {
                Debug.Log("GrinderService: Cannot process spin");
                return;
            }
            
            state.IncrementSpinCount();
            OnSpinCompleted?.Invoke(state.SpinCount);
            
            int requiredSpins = logic.GetRequiredSpins(upgradeLevel);
            Debug.Log($"GrinderService: Spin {state.SpinCount}/{requiredSpins}");
            
            if (state.HasRequiredSpins(requiredSpins))
            {
                Debug.Log("GrinderService: Required spins reached, processing grinding");
                ProcessGrinding();
            }
        }
        
        public void OnButtonPressed()
        {
            Debug.Log($"GrinderService: Button pressed. {state.GetStateInfo()}");
            
            if (upgradeLevel < 1 || !HasBeans)
            {
                Debug.Log("GrinderService: Cannot process button press");
                return;
            }
            
            // For level 1, start continuous processing with delay like level 2
            if (upgradeLevel == 1)
            {
                shouldContinueProcessing = true;
                if (!state.IsProcessing && !isWaitingForAutoProcess)
                {
                    Debug.Log("GrinderService: Starting level 1 continuous processing with delay");
                    isWaitingForAutoProcess = true;
                    autoProcessTimer = 0f;
                }
            }
            else
            {
                ProcessGrinding();
            }
        }
        
        public void ProcessUpdate(float deltaTime)
        {
            // Handle processing progress for all levels
            if (CurrentState == MachineState.Processing)
            {
                float processTime = logic.GetProcessTime(upgradeLevel);
                if (processTime > 0)
                {
                    processProgress += deltaTime / processTime;
                    UpdateProgress(processProgress);
                    
                    if (processProgress >= 1.0f)
                    {
                        Debug.Log("GrinderService: Processing complete via time update");
                        CompleteProcessing();
                    }
                }
            }
            
            // Handle auto-processing delay timer for level 1 and 2
            if ((upgradeLevel == 1 || upgradeLevel == 2) && isWaitingForAutoProcess)
            {
                autoProcessTimer += deltaTime;
                float processDelay = logic.GetProcessDelay(upgradeLevel);
                
                Debug.Log($"GrinderService: Level {upgradeLevel} process timer: {autoProcessTimer:F2}/{processDelay:F2}");
                
                if (autoProcessTimer >= processDelay)
                {
                    Debug.Log($"GrinderService: Level {upgradeLevel} process delay completed, starting grinding");
                    autoProcessTimer = 0f;
                    isWaitingForAutoProcess = false;
                    
                    // Start the actual grinding process
                    ProcessGrinding();
                }
            }
        }
        
        public void OnGroundCoffeeRemoved()
        {
            Debug.Log("GrinderService: Ground coffee removed");
            state.RemoveCoffee();
            
            // If we were waiting due to max coffee size, resume processing for level 1
            if (upgradeLevel == 1 && shouldContinueProcessing && HasBeans && !state.IsProcessing && !isWaitingForAutoProcess)
            {
                Debug.Log("GrinderService: Resuming level 1 processing after coffee removal with delay");
                isWaitingForAutoProcess = true;
                autoProcessTimer = 0f;
            }
            // For level 2, check if we should auto-process remaining beans
            else if (upgradeLevel == 2 && HasBeans && !state.IsProcessing && !isWaitingForAutoProcess)
            {
                Debug.Log("GrinderService: Level 2 - Checking for beans to auto-process after coffee removal");
                CheckAutoProcess();
            }
        }
        
        public void CheckAutoProcess()
        {
            Debug.Log($"GrinderService: CheckAutoProcess called - Level: {upgradeLevel}, HasBeans: {HasBeans}, IsProcessing: {state.IsProcessing}, IsWaiting: {isWaitingForAutoProcess}");
            
            if (upgradeLevel == 2 && logic.ShouldAutoProcess(upgradeLevel, state.CurrentBeans, state.HasExistingCoffee, state.CurrentCoffeeSize))
            {
                if (!state.IsProcessing && !isWaitingForAutoProcess)
                {
                    Debug.Log("GrinderService: Starting auto-process delay");
                    isWaitingForAutoProcess = true;
                    autoProcessTimer = 0f;
                }
                else
                {
                    Debug.Log($"GrinderService: Cannot start auto-process - IsProcessing: {state.IsProcessing}, IsWaiting: {isWaitingForAutoProcess}");
                }
            }
            else
            {
                Debug.Log($"GrinderService: Auto-process conditions not met - Level: {upgradeLevel}, ShouldAutoProcess: {logic.ShouldAutoProcess(upgradeLevel, state.CurrentBeans, state.HasExistingCoffee, state.CurrentCoffeeSize)}");
            }
        }
        
        #endregion
        
        #region Processing Logic
        
        private bool CanProcessSpin()
        {
            return upgradeLevel == 0 && HasBeans && !state.IsProcessing &&
                   logic.CanGrind(state.CurrentBeans, state.HasExistingCoffee, state.CurrentCoffeeSize);
        }
        
        private void ProcessGrinding()
        {
            if (state.IsProcessing)
            {
                Debug.Log("GrinderService: Already processing, ignoring request");
                return;
            }
            
            var grindResult = logic.ProcessGrinding(state.CurrentBeans, state.HasExistingCoffee, state.CurrentCoffeeSize);
            
            if (!grindResult.IsSuccess)
            {
                Debug.Log($"GrinderService: Grinding failed - {grindResult.Message}");
                NotifyUser(grindResult.Message);
                return;
            }
            
            Debug.Log($"GrinderService: {grindResult.Message}");
            
            // Start processing
            state.SetProcessing(true);
            OnGrindingStarted?.Invoke();
            
            // Update bean count
            state.SetBeanCount(grindResult.Data.NewBeanCount);
            
            // Update coffee state
            state.SetCoffeeState(true, grindResult.Data.ResultSize, grindResult.Data.IsNewCoffee);
            
            // Reset spin count after successful processing
            state.ResetSpinCount();
            
            // Handle different upgrade levels
            if (upgradeLevel >= 1)
            {
                // For button/auto modes, start timed processing
                StartProcessing();
            }
            else
            {
                // For manual mode, complete immediately
                CompleteProcessing();
            }
            
            NotifyUser(grindResult.Message);
        }
        
        public override void StartProcessing()
        {
            if (!CanProcess() || state.IsProcessing) return;
            
            base.StartProcessing();
            state.SetProcessing(true);
            
            Debug.Log($"GrinderService: Started processing with {logic.GetProcessTime(upgradeLevel)}s duration");
        }
        
        protected override void CompleteProcessing()
        {
            base.CompleteProcessing();
            state.SetProcessing(false);
            
            Debug.Log("GrinderService: Processing completed");
            
            // Check if we should continue processing for level 1
            if (upgradeLevel == 1 && shouldContinueProcessing)
            {
                // Check if we can continue grinding
                if (HasBeans && logic.CanGrind(state.CurrentBeans, state.HasExistingCoffee, state.CurrentCoffeeSize))
                {
                    Debug.Log("GrinderService: Continuing processing for level 1 with delay");
                    // Use the same delay system as level 2
                    isWaitingForAutoProcess = true;
                    autoProcessTimer = 0f;
                }
                else
                {
                    Debug.Log("GrinderService: Stopping continuous processing - no more beans or coffee at max size");
                    shouldContinueProcessing = false;
                }
            }
            // For level 2, check if we should auto-process the next bean
            else if (upgradeLevel == 2)
            {
                // Trigger auto-process check which will start the delay timer
                CheckAutoProcess();
            }
        }
        
        #endregion
        
        #region State Management
        
        public void ForceReadyState()
        {
            if (HasBeans && CurrentState != MachineState.Ready && !state.IsProcessing)
            {
                Debug.Log($"GrinderService: Force transitioning from {CurrentState} to Ready");
                state.SetState(MachineState.Ready);
            }
        }
        
        public override bool CanProcess()
        {
            return HasBeans && 
                   logic.CanGrind(state.CurrentBeans, state.HasExistingCoffee, state.CurrentCoffeeSize) &&
                   !state.IsProcessing;
        }
        
        #endregion
        
        #region Upgrade Handling
        
        protected override void OnUpgradeLevelChanged(int level)
        {
            Debug.Log($"GrinderService: Upgrade level changed to {level}");
            
            string message = level switch
            {
                0 => "Basic grinder: Use the lever to grind beans.",
                1 => "Grinder upgraded to level 1! Now uses button press.",
                2 => "Grinder upgraded to level 2! Now automatically grinds all beans.",
                _ => $"Grinder upgraded to level {level}!"
            };
            
            NotifyUser(message);
            
            // Check for existing beans when upgrading to level 2
            if (level == 2 && HasBeans)
            {
                Debug.Log("GrinderService: Level 2 upgrade - checking existing beans for auto-processing");
                CheckAutoProcess();
            }
        }
        
        #endregion
        
        #region Public Control Methods
        
        /// <summary>
        /// Stop continuous processing (for level 1)
        /// </summary>
        public void StopContinuousProcessing()
        {
            shouldContinueProcessing = false;
            isWaitingForAutoProcess = false;
            autoProcessTimer = 0f;
            Debug.Log("GrinderService: Continuous processing stopped manually");
        }
        
        #endregion
        
        #region Cleanup
        
        public override void Reset()
        {
            base.Reset();
            state.SetBeanCount(0);
            state.RemoveCoffee();
            state.SetProcessing(false);
            shouldContinueProcessing = false;
            autoProcessTimer = 0f;
            isWaitingForAutoProcess = false;
        }
        
        #endregion
    }
}
