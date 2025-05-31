using System;
using UnityEngine;
using ProjectCoffee.Services.Interfaces;
using ProjectCoffee.Core;
using CoreServices = ProjectCoffee.Core.Services;

namespace ProjectCoffee.Services
{
    /// <summary>
    /// Base service for managing machine state and logic
    /// </summary>
    public abstract class MachineService : IMachineService
    {
        // Events for UI to subscribe to
        public event Action<MachineState> OnStateChanged;
        public event Action<int> OnUpgradeApplied;
        public event Action<float> OnProgressChanged;
        public event Action OnProcessCompleted;
        public event Action<string> OnNotificationRequested;
        
        protected MachineConfig config;
        protected MachineState currentState = MachineState.Idle;
        protected int upgradeLevel = 0;
        protected float processProgress = 0f;
        
        public MachineState CurrentState => currentState;
        public int UpgradeLevel => upgradeLevel;
        public float ProcessProgress => processProgress;
        
        protected MachineService(MachineConfig config)
        {
            this.config = config;
        }
        
        /// <summary>
        /// Set the upgrade level and apply its effects
        /// </summary>
        public virtual void SetUpgradeLevel(int level)
        {
            if (level < 0 || level > config.maxUpgradeLevel)
                return;
                
            upgradeLevel = level;
            OnUpgradeApplied?.Invoke(level);
            OnUpgradeLevelChanged(level);
        }
        
        /// <summary>
        /// Override to handle upgrade-specific changes
        /// </summary>
        protected virtual void OnUpgradeLevelChanged(int level) { }
        
        /// <summary>
        /// Transition to a new state
        /// </summary>
        protected void TransitionTo(MachineState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                OnStateChanged?.Invoke(newState);
            }
        }
        
        /// <summary>
        /// Request notification display
        /// </summary>
        protected void NotifyUser(string message)
        {
            OnNotificationRequested?.Invoke(message);
            
            var notificationService = CoreServices.Notification;
            if (notificationService != null)
            {
                notificationService.ShowNotification(message);
            }
            else if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowNotification(message);
            }
        }
        
        /// <summary>
        /// Update process progress
        /// </summary>
        protected void UpdateProgress(float progress)
        {
            processProgress = Mathf.Clamp01(progress);
            OnProgressChanged?.Invoke(processProgress);
        }
        
        /// <summary>
        /// Check if the machine can process
        /// </summary>
        public virtual bool CanProcess()
        {
            return currentState == MachineState.Ready;
        }
        
        /// <summary>
        /// Start processing
        /// </summary>
        public virtual void StartProcessing()
        {
            if (!CanProcess()) return;
            
            TransitionTo(MachineState.Processing);
            processProgress = 0f;
        }
        
        /// <summary>
        /// Complete processing
        /// </summary>
        protected virtual void CompleteProcessing()
        {
            TransitionTo(MachineState.Complete);
            OnProcessCompleted?.Invoke();
        }
        
        /// <summary>
        /// Reset machine to idle state
        /// </summary>
        public virtual void Reset()
        {
            TransitionTo(MachineState.Idle);
            processProgress = 0f;
        }
    }
    
    /// <summary>
    /// Machine states
    /// </summary>
    public enum MachineState
    {
        Idle,        // Machine is empty and waiting
        Ready,       // Machine has required inputs and can process
        Processing,  // Machine is actively processing
        Complete,    // Process completed, waiting for output removal
        Error        // Error state
    }
}
