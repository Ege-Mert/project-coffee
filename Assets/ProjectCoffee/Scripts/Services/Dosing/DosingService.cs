using System;
using ProjectCoffee.Services.Interfaces;
using ProjectCoffee.Services;
using ProjectCoffee.Machines.Dosing.Logic;
using ProjectCoffee.Core;
using UnityEngine;

namespace ProjectCoffee.Services.Dosing
{
    /// <summary>
    /// Clean dosing service that coordinates between logic and state.
    /// No Unity dependencies - pure coordination and event publishing.
    /// </summary>
    public class DosingService : MachineService, IDosingService
    {
        #region Events

        // Specific dosing events
        public event Action<float> OnCoffeeAmountChanged;
        public event Action<float> OnPortafilterFillChanged;
        public event Action<QualityResult> OnQualityEvaluated;
        public event Action OnAutoDoseStarted;
        public event Action OnAutoDoseCompleted;
        public event Action<bool> OnPortafilterPresenceChanged;

        #endregion

        #region Private Fields

        private readonly DosingLogic logic;
        private readonly DosingState state;

        #endregion

        #region Properties

        public float StoredCoffeeAmount => state.StoredCoffeeAmount;
        public bool HasPortafilter => state.HasPortafilter;
        public float PortafilterCoffeeAmount => state.PortafilterCoffeeAmount;
        public float MaxStorageCapacity => ((DosingMachineConfig)config).maxStorageCapacity;

        #endregion

        #region Constructor

        public DosingService(DosingMachineConfig config) : base(config)
        {
            logic = new DosingLogic(config);
            state = new DosingState();
            
            SubscribeToStateEvents();
        }

        #endregion

        #region Initialization

        private void SubscribeToStateEvents()
        {
            state.OnStoredCoffeeChanged += amount => OnCoffeeAmountChanged?.Invoke(amount);
            state.OnPortafilterAmountChanged += amount => OnPortafilterFillChanged?.Invoke(amount);
            state.OnPortafilterPresenceChanged += present => OnPortafilterPresenceChanged?.Invoke(present);
            state.OnMachineStateChanged += newState => TransitionTo(newState);
            state.OnQualityEvaluated += quality => OnQualityEvaluated?.Invoke(quality);
        }

        #endregion

        #region IDosingService Implementation

        /// <summary>
        /// Add coffee to storage
        /// </summary>
        public bool AddCoffee(float amount)
        {
            if (!logic.CanAddCoffeeToStorage(state.StoredCoffeeAmount, amount))
            {
                NotifyUser("Cannot add coffee to storage!");
                return false;
            }

            float addableAmount = logic.CalculateAddableAmount(state.StoredCoffeeAmount, amount);
            state.AddStoredCoffee(addableAmount);

            string message = addableAmount < amount 
                ? $"Added {addableAmount:F1}g to storage. Storage full!" 
                : $"Added {addableAmount:F1}g to storage. Total: {state.StoredCoffeeAmount:F1}g";
            
            NotifyUser(message);
            UpdateMachineState();

            return true;
        }

        /// <summary>
        /// Set portafilter presence
        /// </summary>
        public void SetPortafilterPresent(bool present)
        {
            bool wasPresent = state.HasPortafilter;
            
            Debug.Log($"DosingService: SetPortafilterPresent({present}) - Was present: {wasPresent}, Current state: {state.CurrentState}");
            
            state.SetPortafilterPresence(present);
            
            // When a new portafilter is placed, ensure proper state transition
            if (!wasPresent && present)
            {
                Debug.Log("DosingService: New portafilter placed, forcing state reset");
                
                // Force machine to Ready state if conditions are met
                if (state.StoredCoffeeAmount > 0)
                {
                    Debug.Log("DosingService: Forcing transition to Ready state");
                    state.SetMachineState(MachineState.Ready);
                }
                else
                {
                    Debug.Log("DosingService: No coffee in storage, staying in Idle");
                    state.SetMachineState(MachineState.Idle);
                }
            }
            else if (wasPresent && !present)
            {
                Debug.Log("DosingService: Portafilter removed, updating state");
                UpdateMachineState();
            }
            
            Debug.Log($"DosingService: SetPortafilterPresent complete - Final state: {state.CurrentState}");
        }

        /// <summary>
        /// Handle manual dispensing (level 0)
        /// </summary>
        public void OnDispensingHold(float deltaTime)
        {
            if (!CanDispense()) return;

            float dispenseAmount = logic.CalculateDispenseAmount(deltaTime, upgradeLevel);
            dispenseAmount = UnityEngine.Mathf.Min(dispenseAmount, state.StoredCoffeeAmount);

            if (dispenseAmount > 0)
            {
                state.TransferCoffee(dispenseAmount);
                
                if (!state.IsProcessing)
                {
                    state.SetProcessingState(true);
                    state.SetMachineState(MachineState.Processing);
                }
            }
        }

        /// <summary>
        /// Handle manual dispensing release (level 0)
        /// </summary>
        public void OnDispensingRelease()
        {
            if (!state.IsProcessing) return;

            state.SetProcessingState(false);
            EvaluateAndCompleteProcess();
        }

        /// <summary>
        /// Clear portafilter
        /// </summary>
        public void ClearPortafilter()
        {
            state.ClearPortafilter();
            UpdateMachineState();
        }

        /// <summary>
        /// Check if auto-dosing should occur (level 2)
        /// </summary>
        public bool ShouldAutoDose()
        {
            return logic.ShouldAutoDose(
                upgradeLevel, 
                state.HasPortafilter, 
                state.StoredCoffeeAmount, 
                state.PortafilterCoffeeAmount
            );
        }

        /// <summary>
        /// Perform auto-dosing calculation and execution
        /// </summary>
        public DosingCalculation PerformAutoDose()
        {
            OnAutoDoseStarted?.Invoke();
            
            var calculation = logic.CalculateAutoDose(
                state.PortafilterCoffeeAmount, 
                state.StoredCoffeeAmount
            );

            if (calculation.AmountToDispense > 0)
            {
                state.TransferCoffee(calculation.AmountToDispense);
                
                string message = calculation.WillReachIdeal 
                    ? $"Auto-dosed to perfect {calculation.ResultingAmount:F1}g!"
                    : $"Added {calculation.AmountToDispense:F1}g. Total: {calculation.ResultingAmount:F1}g";
                
                NotifyUser(message);
                EvaluateAndCompleteProcess();
            }
            
            OnAutoDoseCompleted?.Invoke();
            return calculation;
        }

        /// <summary>
        /// Start button-based processing (level 1)
        /// </summary>
        public bool StartButtonProcess()
        {
            Debug.Log($"DosingService: StartButtonProcess called - Level: {upgradeLevel}, State: {state.CurrentState}, Processing: {state.IsProcessing}");
            
            if (!logic.CanPerformOperation(upgradeLevel, DosingOperation.ButtonPress))
            {
                Debug.Log("DosingService: Cannot perform button operation for current level");
                return false;
            }

            if (!state.CanStartProcessing())
            {
                Debug.Log($"DosingService: Cannot start processing - HasPortafilter: {state.HasPortafilter}, Storage: {state.StoredCoffeeAmount}, Processing: {state.IsProcessing}, State: {state.CurrentState}");
                return false;
            }

            Debug.Log("DosingService: Starting button process - setting processing state");
            state.SetProcessingState(true);
            state.SetMachineState(MachineState.Processing);
            
            return true;
        }

        /// <summary>
        /// Complete button-based processing (level 1)
        /// </summary>
        public void CompleteButtonProcess()
        {
            Debug.Log($"DosingService: CompleteButtonProcess called - Current portafilter: {state.PortafilterCoffeeAmount}g, Storage: {state.StoredCoffeeAmount}g");
            
            var calculation = logic.CalculateAutoDose(
                state.PortafilterCoffeeAmount, 
                state.StoredCoffeeAmount
            );

            Debug.Log($"DosingService: Auto-dose calculation - AmountToDispense: {calculation.AmountToDispense}g, ResultingAmount: {calculation.ResultingAmount}g");

            if (calculation.AmountToDispense > 0)
            {
                state.TransferCoffee(calculation.AmountToDispense);
                Debug.Log($"DosingService: Transferred {calculation.AmountToDispense}g coffee");
            }

            Debug.Log("DosingService: Setting processing state to false");
            state.SetProcessingState(false);
            EvaluateAndCompleteProcess();
            
            // Ensure we return to ready state if conditions are met
            UpdateMachineState();
            
            Debug.Log($"DosingService: CompleteButtonProcess finished - Final state: {state.CurrentState}");
        }

        #endregion

        #region State Management

        private void UpdateMachineState()
        {
            // Don't change state if we're currently processing
            if (state.IsProcessing)
            {
                if (state.CurrentState != MachineState.Processing)
                {
                    state.SetMachineState(MachineState.Processing);
                }
                return;
            }
            
            // If we have portafilter and storage, should be ready
            if (state.ShouldBeReady() && state.CurrentState != MachineState.Ready)
            {
                Debug.Log($"DosingService: Transitioning to Ready - HasPortafilter: {state.HasPortafilter}, Storage: {state.StoredCoffeeAmount}");
                state.SetMachineState(MachineState.Ready);
            }
            // If missing requirements, should be idle
            else if (!state.ShouldBeReady() && state.CurrentState == MachineState.Ready)
            {
                Debug.Log($"DosingService: Transitioning to Idle - HasPortafilter: {state.HasPortafilter}, Storage: {state.StoredCoffeeAmount}");
                state.SetMachineState(MachineState.Idle);
            }
            // If portafilter has coffee and we're not processing, should be complete
            else if (state.PortafilterCoffeeAmount > 0 && !state.IsProcessing && state.CurrentState != MachineState.Complete)
            {
                Debug.Log($"DosingService: Transitioning to Complete - Portafilter has {state.PortafilterCoffeeAmount}g");
                state.SetMachineState(MachineState.Complete);
            }
        }

        private bool CanDispense()
        {
            return state.HasPortafilter && 
                   state.StoredCoffeeAmount > 0 && 
                   logic.CanPerformOperation(upgradeLevel, DosingOperation.ManualHold);
        }

        private void EvaluateAndCompleteProcess()
        {
            if (state.PortafilterCoffeeAmount > 0)
            {
                var qualityResult = logic.EvaluateQuality(state.PortafilterCoffeeAmount);
                state.SetQualityResult(qualityResult);
                
                NotifyUser($"{qualityResult.Description} coffee weight: {qualityResult.Amount:F1}g");
                state.SetMachineState(MachineState.Complete);
            }
            else
            {
                UpdateMachineState();
            }
            
            Debug.Log($"DosingService: Process evaluation complete - State: {state.CurrentState}, PortafilterAmount: {state.PortafilterCoffeeAmount}g");
        }

        #endregion

        #region Processing Time

        public float GetProcessingTime()
        {
            return logic.GetProcessingTime(upgradeLevel);
        }

        public InteractionType GetInteractionType()
        {
            return logic.GetInteractionType(upgradeLevel);
        }

        #endregion

        #region Base Overrides

        public override bool CanProcess()
        {
            return state.CanStartProcessing();
        }

        public override void StartProcessing()
        {
            base.StartProcessing();
            state.SetProcessingState(true);
        }

        protected override void CompleteProcessing()
        {
            base.CompleteProcessing();
            state.SetProcessingState(false);
        }

        protected override void OnUpgradeLevelChanged(int level)
        {
            string message = level switch
            {
                0 => "Manual dosing: Hold button to dispense coffee",
                1 => "Semi-Auto dosing: Press button for perfect dose",
                2 => "Fully Automatic: Auto-doses when portafilter is placed",
                _ => $"Dosing machine upgraded to level {level}!"
            };

            NotifyUser(message);
        }

        public override void Reset()
        {
            base.Reset();
            state.Reset();
        }

        #endregion

        #region Public Access (for Machine)

        /// <summary>
        /// Force state transition (for machine coordination)
        /// </summary>
        public void ForceStateTransition(MachineState newState)
        {
            state.SetMachineState(newState);
        }

        /// <summary>
        /// Get current state info for debugging
        /// </summary>
        public string GetStateInfo()
        {
            return state.GetStateInfo();
        }

        #endregion
    }
}
