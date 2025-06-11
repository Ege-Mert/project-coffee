using System;
using ProjectCoffee.Services;
using ProjectCoffee.Core;
using ProjectCoffee.Machines.Dosing.Logic;
using UnityEngine;

namespace ProjectCoffee.Machines.Dosing.Logic
{
    /// <summary>
    /// Manages all state for the dosing machine.
    /// Provides events for state changes and encapsulates data access.
    /// </summary>
    public class DosingState
    {
        #region Events

        public event Action<float> OnStoredCoffeeChanged;
        public event Action<float> OnPortafilterAmountChanged;
        public event Action<bool> OnPortafilterPresenceChanged;
        public event Action<MachineState> OnMachineStateChanged;
        public event Action<QualityResult> OnQualityEvaluated;
        public event Action<bool> OnProcessingStateChanged;

        #endregion

        #region Private Fields

        private float storedCoffeeAmount = 0f;
        private float portafilterCoffeeAmount = 0f;
        private bool hasPortafilter = false;
        private MachineState currentState = MachineState.Idle;
        private bool isProcessing = false;
        private QualityResult? lastQualityResult = null;

        #endregion

        #region Public Properties

        public float StoredCoffeeAmount => storedCoffeeAmount;
        public float PortafilterCoffeeAmount => portafilterCoffeeAmount;
        public bool HasPortafilter => hasPortafilter;
        public MachineState CurrentState => currentState;
        public bool IsProcessing => isProcessing;
        public QualityResult? LastQualityResult => lastQualityResult;

        #endregion

        #region State Modification Methods

        /// <summary>
        /// Update stored coffee amount
        /// </summary>
        public void SetStoredCoffeeAmount(float amount)
        {
            if (amount != storedCoffeeAmount)
            {
                storedCoffeeAmount = UnityEngine.Mathf.Max(0, amount);
                OnStoredCoffeeChanged?.Invoke(storedCoffeeAmount);
            }
        }

        /// <summary>
        /// Add coffee to storage
        /// </summary>
        public void AddStoredCoffee(float amount)
        {
            SetStoredCoffeeAmount(storedCoffeeAmount + amount);
        }

        /// <summary>
        /// Remove coffee from storage
        /// </summary>
        public void RemoveStoredCoffee(float amount)
        {
            SetStoredCoffeeAmount(storedCoffeeAmount - amount);
        }

        /// <summary>
        /// Update portafilter coffee amount
        /// </summary>
        public void SetPortafilterAmount(float amount)
        {
            if (amount != portafilterCoffeeAmount)
            {
                portafilterCoffeeAmount = UnityEngine.Mathf.Max(0, amount);
                OnPortafilterAmountChanged?.Invoke(portafilterCoffeeAmount);
            }
        }

        /// <summary>
        /// Add coffee to portafilter
        /// </summary>
        public void AddToPortafilter(float amount)
        {
            SetPortafilterAmount(portafilterCoffeeAmount + amount);
        }

        /// <summary>
        /// Clear portafilter
        /// </summary>
        public void ClearPortafilter()
        {
            SetPortafilterAmount(0f);
        }

        /// <summary>
        /// Set portafilter presence
        /// </summary>
        public void SetPortafilterPresence(bool present)
        {
            if (present != hasPortafilter)
            {
                hasPortafilter = present;
                
                // Clear portafilter amount when removed OR when new one is placed
                if (!present)
                {
                    Debug.Log("DosingState: Portafilter removed, clearing amount");
                    ClearPortafilter();
                }
                else
                {
                    Debug.Log("DosingState: New portafilter detected, clearing previous amount");
                    ClearPortafilter(); // Clear any previous amount for fresh start
                }
                
                OnPortafilterPresenceChanged?.Invoke(hasPortafilter);
            }
        }

        /// <summary>
        /// Set machine state
        /// </summary>
        public void SetMachineState(MachineState newState)
        {
            if (newState != currentState)
            {
                currentState = newState;
                OnMachineStateChanged?.Invoke(currentState);
            }
        }

        /// <summary>
        /// Set processing state
        /// </summary>
        public void SetProcessingState(bool processing)
        {
            if (processing != isProcessing)
            {
                isProcessing = processing;
                OnProcessingStateChanged?.Invoke(isProcessing);
            }
        }

        /// <summary>
        /// Set quality result
        /// </summary>
        public void SetQualityResult(QualityResult qualityResult)
        {
            lastQualityResult = qualityResult;
            OnQualityEvaluated?.Invoke(qualityResult);
        }

        #endregion

        #region Transfer Operations

        /// <summary>
        /// Transfer coffee from storage to portafilter
        /// </summary>
        public bool TransferCoffee(float amount)
        {
            if (amount <= 0 || amount > storedCoffeeAmount)
                return false;

            RemoveStoredCoffee(amount);
            AddToPortafilter(amount);
            return true;
        }

        #endregion

        #region State Queries

        /// <summary>
        /// Check if machine can start processing
        /// </summary>
        public bool CanStartProcessing()
        {
            return hasPortafilter && 
                   storedCoffeeAmount > 0 && 
                   !isProcessing && 
                   (currentState == MachineState.Ready || 
                    currentState == MachineState.Idle ||
                    currentState == MachineState.Complete);
        }

        /// <summary>
        /// Check if ready state conditions are met
        /// </summary>
        public bool ShouldBeReady()
        {
            return hasPortafilter && storedCoffeeAmount > 0 && !isProcessing;
        }

        /// <summary>
        /// Get current state summary for debugging
        /// </summary>
        public string GetStateInfo()
        {
            return $"State: {currentState}, Storage: {storedCoffeeAmount:F1}g, " +
                   $"Portafilter: {portafilterCoffeeAmount:F1}g, Present: {hasPortafilter}, " +
                   $"Processing: {isProcessing}";
        }

        #endregion

        #region Reset and Cleanup

        /// <summary>
        /// Reset all state to initial values
        /// </summary>
        public void Reset()
        {
            SetStoredCoffeeAmount(0f);
            ClearPortafilter();
            SetPortafilterPresence(false);
            SetMachineState(MachineState.Idle);
            SetProcessingState(false);
            lastQualityResult = null;
        }

        #endregion
    }
}
