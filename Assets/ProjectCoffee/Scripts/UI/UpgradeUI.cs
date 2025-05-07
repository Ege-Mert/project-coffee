using UnityEngine;
using UnityEngine.UI;
using ProjectCoffee.Core;
using ProjectCoffee.Core.Services;
using ProjectCoffee.Services.Interfaces;
using System.Collections.Generic;
using TMPro;

namespace ProjectCoffee.UI
{
    /// <summary>
    /// UI panel for displaying and purchasing upgrades
    /// </summary>
    public class UpgradeUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject upgradePanel;
        [SerializeField] private Transform upgradesContainer;
        [SerializeField] private UpgradeItemUI upgradeItemPrefab;
        [SerializeField] private TMP_Text moneyText;
        [SerializeField] private Button closeButton;

        private List<UpgradeItemUI> upgradeItems = new List<UpgradeItemUI>();

        private void Start()
        {
        if (closeButton != null)
        {
        closeButton.onClick.AddListener(Hide);
        }

        // Start hidden
        if (upgradePanel != null)
        {
        upgradePanel.SetActive(false);
        }

        // Subscribe to event bus for money changes
        EventBus.OnMoneyChanged += OnMoneyChanged;
        
        // Subscribe to service registered event to get notified when services are ready
        EventBus.OnServicesInitialized += OnServicesInitialized;
        
        // Try to subscribe right away in case services are already registered
        TrySubscribeToUpgradeService();
        }

        private void OnDestroy()
        {
        // Unsubscribe from all events
        UnsubscribeFromServices();
        EventBus.OnMoneyChanged -= OnMoneyChanged;
        EventBus.OnServicesInitialized -= OnServicesInitialized;
        }
        
        private void OnServicesInitialized()
        {
        TrySubscribeToUpgradeService();
        }
    
    private void TrySubscribeToUpgradeService()
    {
        // First unsubscribe to avoid double-subscription
        UnsubscribeFromServices();
        
        // Try to get upgrade service from ServiceManager first
        IUpgradeService upgradeService = null;
        
        if (ServiceManager.Instance != null)
        {
            upgradeService = ServiceManager.Instance.GetService<IUpgradeService>();
        }
        // Fallback to ServiceLocator
        else
        {
            upgradeService = ServiceLocator.Instance.GetService<IUpgradeService>();
        }
        
        // Subscribe if service is available
        if (upgradeService != null)
        {
            upgradeService.OnUpgradesChanged += HandleUpgradesChanged;
            Debug.Log("UpgradeUI successfully subscribed to UpgradeService");
        }
        else
        {
            Debug.Log("UpgradeUI could not subscribe to UpgradeService - not available");
        }
    }
    
    private void UnsubscribeFromServices()
    {
        // Try to get upgrade service from ServiceManager first
        IUpgradeService upgradeService = null;
        
        if (ServiceManager.Instance != null)
        {
            upgradeService = ServiceManager.Instance.GetService<IUpgradeService>();
        }
        // Fallback to ServiceLocator
        else
        {
            upgradeService = ServiceLocator.Instance.GetService<IUpgradeService>();
        }
        
        // Unsubscribe if service is available
        if (upgradeService != null)
        {
            upgradeService.OnUpgradesChanged -= HandleUpgradesChanged;
        }
    }

    /// <summary>
    /// Show the upgrade panel
    /// </summary>
    public void Show()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(true);
            RefreshUI();
        }
    }

        /// <summary>
        /// Hide the upgrade panel
        /// </summary>
        public void Hide()
        {
            if (upgradePanel != null)
            {
                upgradePanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// Handle when available upgrades change
        /// </summary>
        private void HandleUpgradesChanged(Dictionary<string, int> upgradeLevels)
        {
            RefreshUI();
        }

        /// <summary>
        /// Refresh the entire UI
        /// </summary>
        private void RefreshUI()
        {
            // Clear existing items
            foreach (var item in upgradeItems)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }
            upgradeItems.Clear();

            // Update money display
            UpdateMoneyDisplay();

            // Try to get upgrade service from ServiceManager first
            IUpgradeService upgradeService = null;
            
            if (ServiceManager.Instance != null)
            {
                upgradeService = ServiceManager.Instance.GetService<IUpgradeService>();
            }
            // Fallback to ServiceLocator
            else
            {
                upgradeService = ServiceLocator.Instance.GetService<IUpgradeService>();
            }
            
            if (upgradeService == null) 
            {
                Debug.Log("UpgradeUI: Cannot refresh - upgrade service not available yet");
                return; // Just return if no service is available yet
            }
            
            // Create upgrade items for each machine
            var machines = upgradeService.GetAllMachines();
            foreach (var machine in machines)
            {
                // If no next upgrade, machine is fully upgraded
                if (machine.IsFullyUpgraded)
                    continue;

                // Create UI item
                var itemGO = Instantiate(upgradeItemPrefab, upgradesContainer);
                var itemUI = itemGO.GetComponent<UpgradeItemUI>();
                
                if (itemUI != null)
                {
                    itemUI.Initialize(machine.machineId, machine.machineName, machine, OnUpgradeClicked);
                    upgradeItems.Add(itemUI);
                }
            }
        }

        /// <summary>
        /// Handle upgrade button click
        /// </summary>
        private void OnUpgradeClicked(string machineId)
        {
            Debug.Log($"UpgradeUI: OnUpgradeClicked called for {machineId}");
            
            // Try to get services via ServiceManager first, then fall back to ServiceLocator
            IUpgradeService upgradeService = null;
            INotificationService notificationService = null;
            
            if (ServiceManager.Instance != null)
            {
                upgradeService = ServiceManager.Instance.GetService<IUpgradeService>();
                notificationService = ServiceManager.Instance.GetService<INotificationService>();
                Debug.Log("UpgradeUI: Attempting to get services from ServiceManager");
            }
            
            // Fall back to ServiceLocator if needed
            if (upgradeService == null)
            {
                Debug.LogWarning("UpgradeUI: UpgradeService not found in ServiceManager, trying ServiceLocator");
                upgradeService = ServiceLocator.Instance.GetService<IUpgradeService>();
            }
            
            if (notificationService == null)
            {
                Debug.LogWarning("UpgradeUI: NotificationService not found in ServiceManager, trying ServiceLocator");
                notificationService = ServiceLocator.Instance.GetService<INotificationService>();
            }
            
            // Check if we have the required services
            if (upgradeService == null)
            {
                Debug.LogError("UpgradeUI: Cannot upgrade - UpgradeService not available");
                return;
            }
            
            // Log service info for debugging
            if (upgradeService != null)
            {
                try
                {
                    upgradeService.LogRegisteredMachines();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"UpgradeUI: Failed to log machines: {ex.Message}");
                }
            }
            
            // Try to purchase upgrade
            bool success = upgradeService.PurchaseUpgrade(machineId);
            Debug.Log($"UpgradeUI: PurchaseUpgrade result for {machineId}: {success}");
            
            if (success)
            {
                // Upgrade purchased successfully, notification handled by service
                Debug.Log($"UpgradeUI: Successfully upgraded {machineId}");
                
                // Refresh UI to show new state
                RefreshUI();
            }
            else if (notificationService != null)
            {
                // Show error - not enough money or already max level
                string message = "Cannot purchase upgrade!";
                notificationService.ShowNotification(message);
                Debug.LogWarning($"UpgradeUI: Failed to upgrade {machineId} - {message}");
            }
        }

        /// <summary>
        /// Handle money changes
        /// </summary>
        private void OnMoneyChanged(int newAmount)
        {
            UpdateMoneyDisplay();
            
            // Update button states
            foreach (var item in upgradeItems)
            {
                if (item != null)
                {
                    item.UpdateAffordability();
                }
            }
        }

        /// <summary>
        /// Update money display
        /// </summary>
        private void UpdateMoneyDisplay()
        {
            if (moneyText != null)
            {
                // Try to get money from ServiceManager first
                int currentMoney = 0;
                IGameService gameService = null;
                
                if (ServiceManager.Instance != null)
                {
                    gameService = ServiceManager.Instance.GetService<IGameService>();
                }
                // Fallback to ServiceLocator
                else
                {
                    gameService = ServiceLocator.Instance.GetService<IGameService>();
                }
                
                // Get money from service if available
                if (gameService != null)
                {
                    currentMoney = gameService.Money;
                }
                // Or fallback to direct GameManager if available
                else if (GameManager.Instance != null)
                {
                    currentMoney = GameManager.Instance.Money;
                }
                
                moneyText.text = $"Money: ${currentMoney}";
            }
        }
    }
}