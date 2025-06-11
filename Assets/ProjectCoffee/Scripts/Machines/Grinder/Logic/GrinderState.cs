using System;
using ProjectCoffee.Services;
using UnityEngine;

namespace ProjectCoffee.Machines.Grinder.Logic
{
    /// <summary>
    /// Manages the state of the grinder machine
    /// </summary>
    public class GrinderState
    {
        public event Action<int> OnBeanCountChanged;
        public event Action<MachineState> OnStateChanged;
        public event Action<GroundCoffee.GrindSize> OnCoffeeCreated;
        public event Action<GroundCoffee.GrindSize> OnCoffeeUpgraded;
        
        private int currentBeans = 0;
        private MachineState currentState = MachineState.Idle;
        private bool hasExistingCoffee = false;
        private GroundCoffee.GrindSize currentCoffeeSize = GroundCoffee.GrindSize.Small;
        private int spinCount = 0;
        private bool isProcessing = false;
        
        #region Properties
        
        public int CurrentBeans => currentBeans;
        public MachineState CurrentState => currentState;
        public bool HasExistingCoffee => hasExistingCoffee;
        public GroundCoffee.GrindSize CurrentCoffeeSize => currentCoffeeSize;
        public int SpinCount => spinCount;
        public bool IsProcessing => isProcessing;
        
        #endregion
        
        #region State Management
        
        /// <summary>
        /// Update bean count and notify listeners
        /// </summary>
        public void SetBeanCount(int newCount)
        {
            if (currentBeans != newCount)
            {
                currentBeans = newCount;
                OnBeanCountChanged?.Invoke(currentBeans);
                UpdateMachineState();
            }
        }
        
        /// <summary>
        /// Set the machine state
        /// </summary>
        public void SetState(MachineState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                OnStateChanged?.Invoke(currentState);
            }
        }
        
        /// <summary>
        /// Set coffee state
        /// </summary>
        public void SetCoffeeState(bool hasExisting, GroundCoffee.GrindSize size, bool isNewCoffee = false)
        {
            bool wasNew = !hasExistingCoffee && hasExisting;
            bool wasUpgraded = hasExistingCoffee && hasExisting && currentCoffeeSize != size;
            
            hasExistingCoffee = hasExisting;
            currentCoffeeSize = size;
            
            if (wasNew || isNewCoffee)
            {
                OnCoffeeCreated?.Invoke(size);
            }
            else if (wasUpgraded)
            {
                OnCoffeeUpgraded?.Invoke(size);
            }
            
            UpdateMachineState();
        }
        
        /// <summary>
        /// Remove existing coffee
        /// </summary>
        public void RemoveCoffee()
        {
            bool hadCoffee = hasExistingCoffee;
            GroundCoffee.GrindSize previousSize = currentCoffeeSize;
            
            hasExistingCoffee = false;
            currentCoffeeSize = GroundCoffee.GrindSize.Small;
            ResetSpinCount();
            
            if (hadCoffee)
            {
                Debug.Log($"GrinderState: Coffee removed (was {previousSize}), current beans: {currentBeans}");
            }
            
            UpdateMachineState();
        }
        
        /// <summary>
        /// Set processing state
        /// </summary>
        public void SetProcessing(bool processing)
        {
            isProcessing = processing;
            if (processing)
            {
                SetState(MachineState.Processing);
            }
            else
            {
                UpdateMachineState();
            }
        }
        
        #endregion
        
        #region Spin Management
        
        /// <summary>
        /// Increment spin count
        /// </summary>
        public void IncrementSpinCount()
        {
            spinCount++;
        }
        
        /// <summary>
        /// Reset spin count
        /// </summary>
        public void ResetSpinCount()
        {
            spinCount = 0;
        }
        
        /// <summary>
        /// Check if required spins are completed
        /// </summary>
        public bool HasRequiredSpins(int requiredSpins)
        {
            return spinCount >= requiredSpins;
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Update machine state based on current conditions
        /// </summary>
        private void UpdateMachineState()
        {
            if (isProcessing)
            {
                // Don't change state while processing
                return;
            }
            
            MachineState newState;
            
            if (currentBeans <= 0)
            {
                newState = MachineState.Idle;
            }
            else if (hasExistingCoffee && currentCoffeeSize == GroundCoffee.GrindSize.Large)
            {
                newState = MachineState.Complete; // Coffee at max size
            }
            else
            {
                newState = MachineState.Ready; // Has beans and can process
            }
            
            SetState(newState);
        }
        
        #endregion
        
        #region Debug/Information
        
        /// <summary>
        /// Get current state information for debugging
        /// </summary>
        public string GetStateInfo()
        {
            return $"Beans: {currentBeans}, State: {currentState}, Coffee: {(hasExistingCoffee ? currentCoffeeSize.ToString() : "None")}, Spins: {spinCount}, Processing: {isProcessing}";
        }
        
        #endregion
    }
}
