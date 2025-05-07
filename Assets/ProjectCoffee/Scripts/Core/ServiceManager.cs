using UnityEngine;
using ProjectCoffee.Services;
using ProjectCoffee.Services.Interfaces;
using ProjectCoffee.Core.Services;

namespace ProjectCoffee.Core
{
    /// <summary>
    /// Persistent manager for all game services that survives across scene loads
    /// </summary>
    public class ServiceManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIManager uiManager;
        [SerializeField] private AudioManager audioManager;
        
        private static ServiceManager _instance;
        private bool _servicesInitialized = false;
        
        private void Awake()
        {
            // Make this object persist across scenes
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize services
            InitializeServices();
        }
        
        private void InitializeServices()
        {
        Debug.Log("ServiceManager: Initializing services...");
        
        var serviceLocator = ServiceLocator.Instance;
        
        // IMPORTANT: Register UIManager service FIRST to ensure notifications work
        if (uiManager != null)
        {
            serviceLocator.RegisterService<IUIService>(uiManager);
            var notificationService = new NotificationService(uiManager);
            serviceLocator.RegisterService<INotificationService>(notificationService);
            Debug.Log("ServiceManager: UIManager and NotificationService registered successfully");
        }
        else
        {
            Debug.LogError("ServiceManager: UIManager reference is missing! UI services will not work properly. Check Inspector references!");
            // Try to find UIManager in scene as fallback
        uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                Debug.Log("ServiceManager: Found UIManager in scene as fallback");
                serviceLocator.RegisterService<IUIService>(uiManager);
                var notificationService = new NotificationService(uiManager);
                serviceLocator.RegisterService<INotificationService>(notificationService);
        }
        }
        
        // Find and register GameManager service
        var gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            serviceLocator.RegisterService<IGameService>(gameManager);
            Debug.Log("ServiceManager: GameManager service registered successfully");
        }
        else
        {
            Debug.LogError("ServiceManager: GameManager not found in scene! Game service will not work properly.");
        }
        
        // Create services that don't require Unity components
        // IMPORTANT: Create UpgradeService AFTER other services it depends on
        var upgradeService = new UpgradeService();
        serviceLocator.RegisterService<IUpgradeService>(upgradeService);
        Debug.Log("ServiceManager: UpgradeService registered successfully");
        
        // Verify critical services
        if (!serviceLocator.HasService<INotificationService>())
        {
            Debug.LogError("ServiceManager: Critical service INotificationService is missing!");
        }
        if (!serviceLocator.HasService<IGameService>())
        {
            Debug.LogError("ServiceManager: Critical service IGameService is missing!");
        }
        if (!serviceLocator.HasService<IUpgradeService>())
        {
            Debug.LogError("ServiceManager: Critical service IUpgradeService is missing!");
        }
            
            // Register AudioManager service if available
            if (audioManager != null)
            {
                // TODO: Create and register IAudioService
            }
            
            // Flag that services are initialized
            _servicesInitialized = true;
            
            // Notify listeners that services are initialized
            EventBus.NotifyServicesInitialized();
            
            Debug.Log("ServiceManager: Services initialized successfully");
        }
        
        /// <summary>
        /// Register machine-specific services
        /// </summary>
        public void RegisterMachineService<T>(T service) where T : class, IMachineService
        {
            if (service != null)
            {
                ServiceLocator.Instance.RegisterService<T>(service);
                Debug.Log($"ServiceManager: Registered machine service of type {typeof(T).Name}");
            }
        }
        
        /// <summary>
        /// Create core services if they don't exist yet
        /// </summary>
        public void EnsureCoreServicesExist()
        {
            var serviceLocator = ServiceLocator.Instance;
            
            // Create UpgradeService if it doesn't exist
            if (!serviceLocator.HasService<IUpgradeService>())
            {
                var upgradeService = new UpgradeService();
                serviceLocator.RegisterService<IUpgradeService>(upgradeService);
                Debug.Log("ServiceManager: Created missing UpgradeService");
            }
            
            // Create NotificationService if it doesn't exist but UIManager does
            if (!serviceLocator.HasService<INotificationService>() && uiManager != null)
            {
                serviceLocator.RegisterService<INotificationService>(new NotificationService(uiManager));
                Debug.Log("ServiceManager: Created missing NotificationService");
            }
        }
        
        /// <summary>
        /// Check if a particular service exists
        /// </summary>
        public bool HasService<T>() where T : class
        {
            return ServiceLocator.Instance.HasService<T>();
        }
        
        /// <summary>
        /// Get a service of the specified type
        /// </summary>
        public T GetService<T>() where T : class
        {
            return ServiceLocator.Instance.GetService<T>();
        }
        
        /// <summary>
        /// Static access to service manager instance
        /// </summary>
        public static ServiceManager Instance => _instance;
        
        /// <summary>
        /// Check if all services have been initialized
        /// </summary>
        public bool ServicesInitialized => _servicesInitialized;
    }
}