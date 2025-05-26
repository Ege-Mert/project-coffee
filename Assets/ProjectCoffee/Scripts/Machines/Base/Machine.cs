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
    
    public abstract class Machine<TService, TConfig> : MonoBehaviour 
        where TService : ProjectCoffee.Services.MachineService 
        where TConfig : MachineConfig
    {
        [Header("Configuration")]
        [SerializeField] protected TConfig config;
        
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
            RegisterMachine();
        }
        
        protected virtual void Start()
        {
            RegisterMachineServices();
            SubscribeToUpgradeEvents();
        }
        
        private void RegisterMachine()
        {
            if (config != null)
            {
                EventBus.NotifyMachineRegistered(config.machineId, config, service?.UpgradeLevel ?? 0);
                Debug.Log($"Registered {config.machineId} with upgrade system, level: {service?.UpgradeLevel ?? 0}");
            }
        }
        
        private void RegisterMachineServices()
        {
            if (service == null || config == null) return;
            
            switch (service)
            {
                case IGrinderService grinderService:
                    RegisterMachineService<IGrinderService>(grinderService);
                    break;
                case IGrammingService grammingService:
                    RegisterMachineService<IGrammingService>(grammingService);
                    break;
                case IEspressoMachineService espressoService:
                    RegisterMachineService<IEspressoMachineService>(espressoService);
                    break;
            }
            
            EnsureUpgradeServiceRegistration(config.machineId, config, service.UpgradeLevel);
        }
        
        private void SubscribeToUpgradeEvents()
        {
            if (config == null) return;
            
            EventBus.OnMachineUpgraded -= HandleMachineUpgraded;
            EventBus.OnMachineUpgraded += HandleMachineUpgraded;
        }
        
        private void HandleMachineUpgraded(string machineId, int newLevel)
        {
            if (config == null || machineId != config.machineId) return;
            
            Debug.Log($"{GetType().Name}: Upgrade to level {newLevel}");
            service?.SetUpgradeLevel(newLevel);
        }
        
        private void RegisterMachineService<T>(T service) where T : class, IMachineService
        {
            if (ServiceManager.Instance != null)
                ServiceManager.Instance.RegisterMachineService<T>(service);
            else
                ServiceLocator.Instance.RegisterService<T>(service);
        }
        
        private void EnsureUpgradeServiceRegistration(string machineId, MachineConfig config, int level)
        {
            IUpgradeService upgradeService = ServiceManager.Instance?.GetService<IUpgradeService>() 
                ?? ServiceLocator.Instance.GetService<IUpgradeService>();
            
            if (upgradeService != null)
            {
                upgradeService.RegisterMachine(machineId, config, level);
                Debug.Log($"{machineId} registered with UpgradeService, level: {level}");
            }
            else
            {
                EventBus.NotifyMachineRegistered(machineId, config, level);
                Debug.Log($"{machineId} registered via EventBus, level: {level}");
            }
        }
        
        protected virtual void ValidateConfiguration()
        {
            if (config == null)
                Debug.LogError($"{GetType().Name}: Configuration is missing!");
        }
        
        protected abstract void InitializeService();
        
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
            if (service != null)
            {
                service.OnStateChanged -= HandleStateChanged;
                service.OnProgressChanged -= HandleProgressChanged;
                service.OnProcessCompleted -= HandleProcessCompleted;
                service.OnNotificationRequested -= HandleNotification;
                service.OnUpgradeApplied -= HandleUpgradeApplied;
            }
            
            EventBus.OnMachineUpgraded -= HandleMachineUpgraded;
        }
        
        protected virtual void HandleStateChanged(MachineState newState)
        {
            OnStateChanged?.Invoke(newState);
        }
        
        protected virtual void HandleProgressChanged(float progress)
        {
            OnProgressUpdated?.Invoke(progress);
        }
        
        protected virtual void HandleProcessCompleted()
        {
            OnProcessingCompleted?.Invoke();
        }
        
        protected virtual void HandleNotification(string message)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowNotification(message);
        }
        
        protected virtual void HandleUpgradeApplied(int level)
        {
            Debug.Log($"{GetType().Name} upgraded to level {level}");
        }
        
        public virtual void SetUpgradeLevel(int level)
        {
            service?.SetUpgradeLevel(level);
        }
        
        public TService GetService() => service;
    }
}