using System;
using UnityEngine;
using UnityEngine.Events;
using ProjectCoffee.Services.Interfaces;
using ProjectCoffee.Core;
using CoreServices = ProjectCoffee.Core.Services;

namespace ProjectCoffee.Machines
{
    using MachineState = ProjectCoffee.Services.MachineState;
    
    public abstract class MachineBase : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] protected MachineConfig config;
        
        [Header("State")]
        [SerializeField] protected MachineState currentState = MachineState.Idle;
        [SerializeField] protected int upgradeLevel = 0;
        [SerializeField] protected float processProgress = 0f;
        
        [Header("Events")]
        public UnityEvent OnProcessingStarted;
        public UnityEvent OnProcessingCompleted;
        public UnityEvent<float> OnProgressUpdated;
        public UnityEvent<MachineState> OnStateChanged;
        
        public event Action<MachineState> OnStateChanged_Event;
        public event Action<float> OnProgressChanged;
        public event Action OnProcessCompleted;
        public event Action<int> OnUpgradeApplied;
        
        public MachineState CurrentState => currentState;
        public int UpgradeLevel => upgradeLevel;
        public float ProcessProgress => processProgress;
        public MachineConfig Config => config;
        public string MachineId => config?.machineId ?? gameObject.name;
        
        protected virtual void Awake()
        {
            ValidateConfiguration();
        }
        
        protected virtual void Start()
        {
            RegisterMachine();
            SubscribeToEvents();
            InitializeMachine();
        }
        
        protected virtual void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void ValidateConfiguration()
        {
            if (config == null)
            {
                Debug.LogError($"{GetType().Name}: Configuration is missing!");
            }
        }
        
        private void RegisterMachine()
        {
            if (config != null)
            {
                EventBus.NotifyMachineRegistered(config.machineId, config, upgradeLevel);
            }
        }
        
        private void SubscribeToEvents()
        {
            EventBus.OnMachineUpgraded += HandleMachineUpgraded;
        }
        
        private void UnsubscribeFromEvents()
        {
            EventBus.OnMachineUpgraded -= HandleMachineUpgraded;
        }
        
        private void HandleMachineUpgraded(string machineId, int newLevel)
        {
            if (machineId != MachineId) return;
            
            int previousLevel = upgradeLevel;
            upgradeLevel = newLevel;
            
            OnUpgradeApplied?.Invoke(newLevel);
            ApplyUpgrade(previousLevel, newLevel);
        }
        
        protected void TransitionToState(MachineState newState)
        {
            if (currentState == newState) return;
            
            currentState = newState;
            OnStateChanged?.Invoke(newState);
            OnStateChanged_Event?.Invoke(newState);
            EventBus.NotifyMachineStateChanged(MachineId, newState);
        }
        
        protected void UpdateProgress(float progress)
        {
            processProgress = Mathf.Clamp01(progress);
            OnProgressUpdated?.Invoke(processProgress);
            OnProgressChanged?.Invoke(processProgress);
        }
        
        protected void CompleteProcess()
        {
            OnProcessingCompleted?.Invoke();
            OnProcessCompleted?.Invoke();
        }
        
        protected void NotifyUser(string message)
        {
            CoreServices.UI?.ShowNotification(message);
        }
        
        protected float GetProcessTime()
        {
            if (config?.upgradeLevels == null || upgradeLevel >= config.upgradeLevels.Length)
                return config?.baseProcessTime ?? 3f;
            
            return config.baseProcessTime * config.upgradeLevels[upgradeLevel].processTimeMultiplier;
        }
        
        protected InteractionType GetInteractionType()
        {
            if (config?.upgradeLevels == null || upgradeLevel >= config.upgradeLevels.Length)
                return InteractionType.ManualLever;
            
            return config.upgradeLevels[upgradeLevel].interactionType;
        }
        
        public virtual bool CanProcess()
        {
            return currentState == MachineState.Ready;
        }
        
        public virtual void StartProcess()
        {
            if (!CanProcess()) return;
            
            TransitionToState(MachineState.Processing);
            OnProcessingStarted?.Invoke();
            processProgress = 0f;
        }
        
        public virtual void Reset()
        {
            TransitionToState(MachineState.Idle);
            processProgress = 0f;
        }
        
        protected abstract void InitializeMachine();
        protected abstract void ApplyUpgrade(int previousLevel, int newLevel);
    }
}
