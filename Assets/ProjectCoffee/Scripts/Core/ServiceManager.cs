using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectCoffee.Services;
using ProjectCoffee.Services.Interfaces;

namespace ProjectCoffee.Core
{
    public class ServiceManager : MonoBehaviour
    {
        private static ServiceManager _instance;
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        
        [Header("References")]
        [SerializeField] private UIManager uiManager;
        [SerializeField] private AudioManager audioManager;
        
        private static IGameService _gameService;
        private static IUIService _uiService;
        private static IUpgradeService _upgradeService;
        private static INotificationService _notificationService;
        
        public static ServiceManager Instance => _instance;
        
        public static IGameService Game => _gameService ??= GetService<IGameService>();
        public static IUIService UI => _uiService ??= GetService<IUIService>();
        public static IUpgradeService Upgrade => _upgradeService ??= GetService<IUpgradeService>();
        public static INotificationService Notification => _notificationService ??= GetService<INotificationService>();
        
        public static T Get<T>() where T : class => GetService<T>();
        public static bool IsAvailable<T>() where T : class => GetService<T>() != null;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeServices();
        }
        
        private void InitializeServices()
        {
            RegisterCoreServices();
            ClearCache();
            EventBus.NotifyServicesInitialized();
        }
        
        private void RegisterCoreServices()
        {
            if (uiManager != null)
            {
                Register<IUIService>(uiManager);
                Register<INotificationService>(new NotificationService(uiManager));
            }
            else
            {
                uiManager = FindObjectOfType<UIManager>();
                if (uiManager != null)
                {
                    Register<IUIService>(uiManager);
                    Register<INotificationService>(new NotificationService(uiManager));
                }
            }
            
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                Register<IGameService>(gameManager);
            }
            
            Register<IUpgradeService>(new UpgradeService());
        }
        
        public void Register<T>(T service) where T : class
        {
            var type = typeof(T);
            _services[type] = service;
        }
        
        public void RegisterMachineService<T>(T service) where T : class, IMachineService
        {
            Register<T>(service);
        }
        
        private static T GetService<T>() where T : class
        {
            if (_instance == null) return null;
            
            var type = typeof(T);
            return _instance._services.TryGetValue(type, out var service) ? (T)service : null;
        }
        
        public static void ClearCache()
        {
            _gameService = null;
            _uiService = null;
            _upgradeService = null;
            _notificationService = null;
        }
        
        public bool HasService<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }
        
        public T GetServiceInstance<T>() where T : class
        {
            return GetService<T>();
        }
        
        public void EnsureCoreServicesExist()
        {
            bool servicesCreated = false;
            
            if (!HasService<IUpgradeService>())
            {
                Register<IUpgradeService>(new UpgradeService());
                servicesCreated = true;
            }
            
            if (!HasService<INotificationService>() && uiManager != null)
            {
                Register<INotificationService>(new NotificationService(uiManager));
                servicesCreated = true;
            }
            
            if (servicesCreated)
                ClearCache();
        }
    }
    
    public static class Services
    {
        public static IGameService Game => ServiceManager.Game;
        public static IUIService UI => ServiceManager.UI;
        public static IUpgradeService Upgrade => ServiceManager.Upgrade;
        public static INotificationService Notification => ServiceManager.Notification;
        
        public static T Get<T>() where T : class => ServiceManager.Get<T>();
        public static bool IsAvailable<T>() where T : class => ServiceManager.IsAvailable<T>();
        public static void ClearCache() => ServiceManager.ClearCache();
    }
}
