using System;
using ProjectCoffee.Services.Interfaces;
using ProjectCoffee.Services;
using ProjectCoffee.Machines.Dosing.Logic;
using ProjectCoffee.Core;

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
            state.SetPortafilterPresence(present);
            UpdateMachineState();
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
            if (!logic.CanPerformOperation(upgradeLevel, DosingOperation.ButtonPress))
                return false;

            if (!state.CanStartProcessing())
                return false;

            state.SetProcessingState(true);
            state.SetMachineState(MachineState.Processing);
            
            return true;
        }

        /// <summary>
        /// Complete button-based processing (level 1)
        /// </summary>
        public void CompleteButtonProcess()
        {
            var calculation = logic.CalculateAutoDose(
                state.PortafilterCoffeeAmount, 
                state.StoredCoffeeAmount
            );

            if (calculation.AmountToDispense > 0)
            {
                state.TransferCoffee(calculation.AmountToDispense);
            }

            state.SetProcessingState(false);
            EvaluateAndCompleteProcess();
        }

        #endregion

        #region State Management

        private void UpdateMachineState()
        {
            if (state.ShouldBeReady() && state.CurrentState != MachineState.Ready)
            {
                state.SetMachineState(MachineState.Ready);
            }
            else if (!state.ShouldBeReady() && state.CurrentState == MachineState.Ready)
            {
                state.SetMachineState(MachineState.Idle);
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
