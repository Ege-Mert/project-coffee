using System;
using UnityEngine;
using UnityEngine.Events;
using ProjectCoffee.Services;
using ProjectCoffee.Services.Interfaces;
using ProjectCoffee.Core;
using ProjectCoffee.Core.Services;

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
                
            // Register this machine with the UpgradeService via EventBus
            if (config != null)
            {
                EventBus.NotifyMachineRegistered(config.machineId, config, service?.UpgradeLevel ?? 0);
            }
            Debug.Log($"Registering machine {config.machineId} ({config.displayName}) with upgrade system, level: {service?.UpgradeLevel ?? 0}");
        }
        
        protected virtual void Start()
        {
            UpdateVisualState(MachineState.Idle);
            
            // Register machine services only once during Start
            if (service != null && config != null)
            {
                // Register machine-specific services
                switch (service)
                {
                    case IGrinderService grinderService:
                        Debug.Log($"Registering IGrinderService for {config.machineId}");
                        RegisterMachineService<IGrinderService>(grinderService);
                        break;
                    case IGrammingService grammingService:
                        Debug.Log($"Registering IGrammingService for {config.machineId}");
                        RegisterMachineService<IGrammingService>(grammingService);
                        break;
                    case IEspressoMachineService espressoService:
                        Debug.Log($"Registering IEspressoMachineService for {config.machineId}");
                        RegisterMachineService<IEspressoMachineService>(espressoService);
                        break;
                }
                
                // Also register with UpgradeService if available
                EnsureUpgradeServiceRegistration(config.machineId, config, service.UpgradeLevel);
            }
            
            // Subscribe to upgrade events from EventBus
            SubscribeToUpgradeEvents();
        }
        
        /// <summary>
        /// Subscribe to machine upgrade events from EventBus
        /// </summary>
        private void SubscribeToUpgradeEvents()
        {
            if (config == null) return;
            
            // Unsubscribe first to avoid duplicate subscriptions
            EventBus.OnMachineUpgraded -= HandleMachineUpgraded;
            
            // Subscribe to machine upgrade events
            EventBus.OnMachineUpgraded += HandleMachineUpgraded;
            
            Debug.Log($"{GetType().Name} {name}: Subscribed to machine upgrade events for {config.machineId}");
        }
        
        /// <summary>
        /// Handle machine upgrade event from EventBus
        /// </summary>
        private void HandleMachineUpgraded(string machineId, int newLevel)
        {
            // Only respond to events for this machine
            if (config == null || machineId != config.machineId) return;
            
            Debug.Log($"{GetType().Name} {name}: Received upgrade event for {machineId} to level {newLevel}");
            
            // Update the service upgrade level
            service?.SetUpgradeLevel(newLevel);
        }
        
        /// <summary>
        /// Register a machine service with the ServiceManager
        /// </summary>
        private void RegisterMachineService<T>(T service) where T : class, IMachineService
        {
            // Try to register via ServiceManager if available
            if (ServiceManager.Instance != null)
            {
                ServiceManager.Instance.RegisterMachineService<T>(service);
            }
            // Fallback to direct ServiceLocator registration
            else
            {
                ServiceLocator.Instance.RegisterService<T>(service);
            }
        }
        
        /// <summary>
        /// Ensure the machine is registered with the upgrade service
        /// </summary>
        private void EnsureUpgradeServiceRegistration(string machineId, MachineConfig config, int level)
        {
            bool registered = false;
            
            // Add machine type logging for debugging
            Debug.Log($"Registering machine: ID={machineId}, Type={GetType().Name}, Config={config.displayName}, Level={level}");
            
            // Try to get UpgradeService and register directly
            IUpgradeService upgradeService = null;
            
            // Try via ServiceManager first
            if (ServiceManager.Instance != null)
            {
                upgradeService = ServiceManager.Instance.GetService<IUpgradeService>();
                if (upgradeService == null)
                {
                    Debug.LogWarning($"ServiceManager exists but UpgradeService not found when registering {machineId}");
                }
            }
            // Fallback to ServiceLocator
            else
            {
                Debug.LogWarning("ServiceManager not available, using ServiceLocator fallback");
                upgradeService = ServiceLocator.Instance.GetService<IUpgradeService>();
                if (upgradeService == null)
                {
                    Debug.LogError("UpgradeService not found in ServiceLocator either! Check initialization order.");
                }
            }
            
            // Register with service if available
            if (upgradeService != null)
            {
                upgradeService.RegisterMachine(machineId, config, level);
                registered = true;
                Debug.Log($"Machine {machineId} ({GetType().Name}) registered directly with UpgradeService, level: {level}");
            }
            
            // If couldn't register directly, use EventBus
            if (!registered)
            {
                EventBus.NotifyMachineRegistered(machineId, config, level);
                Debug.Log($"Machine {machineId} ({GetType().Name}) registered via EventBus, level: {level}");
            }
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
            
            // Unsubscribe from EventBus
            EventBus.OnMachineUpgraded -= HandleMachineUpgraded;
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
            Debug.Log($"{GetType().Name}: SetUpgradeLevel called with level {level}");
            service?.SetUpgradeLevel(level);
        }
        
        /// <summary>
        /// Get current service state
        /// </summary>
        protected TService GetService() => service;
    }
}