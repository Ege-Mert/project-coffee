using System;
using UnityEngine;
using UnityEngine.Events;
using ProjectCoffee.Services;

namespace ProjectCoffee.Machines
{
    using MachineState = ProjectCoffee.Services.MachineState;
    /// <summary>
    /// Base class for all machines, handles UI and delegates logic to service
    /// </summary>
    public abstract class Machine<TService, TConfig> : MonoBehaviour 
        where TService : ProjectCoffee.Services.MachineService 
        where TConfig : MachineConfig
    {
        [Header("Configuration")]
        [SerializeField] protected TConfig config;
        
        [Header("Visual State Indicators")]
        [SerializeField] protected GameObject idleIndicator;
        [SerializeField] protected GameObject readyIndicator;
        [SerializeField] protected GameObject processingIndicator;
        [SerializeField] protected GameObject completeIndicator;
        
        [Header("Effects")]
        [SerializeField] protected AudioSource processStartSound;
        [SerializeField] protected AudioSource processCompleteSound;
        [SerializeField] protected ParticleSystem processingParticles;
        [SerializeField] protected Animator machineAnimator;
        
        [Header("Events")]
        public UnityEvent OnProcessingStarted;
        public UnityEvent OnProcessingCompleted;
        public UnityEvent<float> OnProgressUpdated;
        public UnityEvent<MachineState> OnStateChanged;
        
        protected TService service;
        
        protected virtual void Awake()
        {
            ValidateConfiguration();
            InitializeService();
            SetupServiceEvents();
        }
        
        protected virtual void Start()
        {
            UpdateVisualState(MachineState.Idle);
        }
        
        /// <summary>
        /// Validate that we have necessary configuration
        /// </summary>
        protected virtual void ValidateConfiguration()
        {
            if (config == null)
            {
                Debug.LogError($"{GetType().Name}: Configuration is missing!");
            }
        }
        
        /// <summary>
        /// Initialize the service
        /// </summary>
        protected abstract void InitializeService();
        
        /// <summary>
        /// Setup event subscriptions from service
        /// </summary>
        protected virtual void SetupServiceEvents()
        {
            if (service != null)
            {
                service.OnStateChanged += HandleStateChanged;
                service.OnProgressChanged += HandleProgressChanged;
                service.OnProcessCompleted += HandleProcessCompleted;
                service.OnNotificationRequested += HandleNotification;
                service.OnUpgradeApplied += HandleUpgradeApplied;
            }
        }
        
        protected virtual void OnDestroy()
        {
            // Unsubscribe from events
            if (service != null)
            {
                service.OnStateChanged -= HandleStateChanged;
                service.OnProgressChanged -= HandleProgressChanged;
                service.OnProcessCompleted -= HandleProcessCompleted;
                service.OnNotificationRequested -= HandleNotification;
                service.OnUpgradeApplied -= HandleUpgradeApplied;
            }
        }
        
        /// <summary>
        /// Handle state change from service
        /// </summary>
        protected virtual void HandleStateChanged(MachineState newState)
        {
            UpdateVisualState(newState);
            OnStateChanged?.Invoke(newState);
            
            // Play appropriate animations
            if (machineAnimator != null)
            {
                machineAnimator.SetTrigger($"To{newState}");
            }
        }
        
        /// <summary>
        /// Handle progress updates from service
        /// </summary>
        protected virtual void HandleProgressChanged(float progress)
        {
            OnProgressUpdated?.Invoke(progress);
        }
        
        /// <summary>
        /// Handle process completion from service
        /// </summary>
        protected virtual void HandleProcessCompleted()
        {
            OnProcessingCompleted?.Invoke();
            
            if (processCompleteSound != null)
            {
                processCompleteSound.Play();
            }
            
            if (processingParticles != null)
            {
                processingParticles.Stop();
            }
        }
        
        /// <summary>
        /// Handle notification requests from service
        /// </summary>
        protected virtual void HandleNotification(string message)
        {
            // Use UIManager to show notifications
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowNotification(message);
            }
        }
        
        /// <summary>
        /// Handle upgrade level changes
        /// </summary>
        protected virtual void HandleUpgradeApplied(int level)
        {
            Debug.Log($"{GetType().Name} upgraded to level {level}");
        }
        
        /// <summary>
        /// Update visual indicators based on state
        /// </summary>
        protected virtual void UpdateVisualState(MachineState state)
        {
            if (idleIndicator != null) idleIndicator.SetActive(state == MachineState.Idle);
            if (readyIndicator != null) readyIndicator.SetActive(state == MachineState.Ready);
            if (processingIndicator != null) processingIndicator.SetActive(state == MachineState.Processing);
            if (completeIndicator != null) completeIndicator.SetActive(state == MachineState.Complete);
        }
        
        /// <summary>
        /// Set upgrade level
        /// </summary>
        public virtual void SetUpgradeLevel(int level)
        {
            service?.SetUpgradeLevel(level);
        }
        
        /// <summary>
        /// Get current service state
        /// </summary>
        protected TService GetService() => service;
    }
}
