using UnityEngine;
using ProjectCoffee.Services;
using ProjectCoffee.Services.Interfaces;
using ProjectCoffee.Core.Services;

namespace ProjectCoffee.Core
{
    /// <summary>
    /// Handles game initialization and service registration
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Services")]
        [SerializeField] private UIManager uiManager;
        [SerializeField] private AudioManager audioManager;
        
        private void Awake()
        {
            // Set up service locator with all required services
            var serviceLocator = ServiceLocator.Instance;
            
            // Register UI services
            if (uiManager != null)
            {
                serviceLocator.RegisterService<IUIService>(uiManager);
                serviceLocator.RegisterService<INotificationService>(new NotificationService(uiManager));
            }
            else
            {
                Debug.LogError("GameBootstrap: UIManager reference is missing! UI services will not work.");
            }
            
            // Register GameManager service
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                serviceLocator.RegisterService<IGameService>(gameManager);
            }
            else
            {
                Debug.LogError("GameBootstrap: GameManager not found in scene! Game service will not work.");
            }
            
            // Register UpgradeService
            var upgradeService = new UpgradeService();
            serviceLocator.RegisterService<IUpgradeService>(upgradeService);
            
            // Register AudioManager service if available
            if (audioManager != null)
            {
                // TODO: Create and register IAudioService
            }
            
            Debug.Log("GameBootstrap: Services registered successfully");
            
            // Notify all listeners that services are initialized
            EventBus.NotifyServicesInitialized();
        }
    }
}