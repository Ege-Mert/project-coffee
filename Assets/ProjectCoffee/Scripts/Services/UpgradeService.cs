using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectCoffee.Core;
using ProjectCoffee.Core.Services;
using ProjectCoffee.Services.Interfaces;

namespace ProjectCoffee.Services
{
    /// <summary>
    /// Service that handles machine upgrades
    /// </summary>
    public class UpgradeService : IUpgradeService
    {
        // Events
        public event Action<string, int> OnMachineUpgraded;
        public event Action<Dictionary<string, int>> OnUpgradesChanged;
        
        // Private fields
        private Dictionary<string, int> upgradeLevels = new Dictionary<string, int>();
        private Dictionary<string, MachineConfig> machineConfigs = new Dictionary<string, MachineConfig>();
        
        // Cached references to other services
        private IGameService gameService;
        private INotificationService notificationService;
        
        // Properties
        public Dictionary<string, int> UpgradeLevels => upgradeLevels;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public UpgradeService()
        {
        // Get services
        gameService = ServiceLocator.Instance.GetService<IGameService>();
        notificationService = ServiceLocator.Instance.GetService<INotificationService>();
        
        if (gameService == null)
        {
                Debug.LogWarning("UpgradeService: IGameService not found during initialization!");
        }
        
        if (notificationService == null)
        {
            Debug.LogWarning("UpgradeService: INotificationService not found during initialization!");
        }
        
        // Register for events from EventBus
        EventBus.OnMachineRegistered += RegisterMachine;
        
        Debug.Log("UpgradeService initialized");
    }
        
        /// <summary>
        /// Register a machine with the upgrade service
        /// </summary>
        public void RegisterMachine(string machineId, MachineConfig config, int currentLevel = 0)
        {
        if (string.IsNullOrEmpty(machineId) || config == null)
        {
        Debug.LogError($"Cannot register machine: Invalid ID or config");
        return;
        }
        
        // Check if already registered
        bool isUpdate = machineConfigs.ContainsKey(machineId);
        string action = isUpdate ? "updated" : "registered";
        
        // Store machine config
        machineConfigs[machineId] = config;
        
        // Set initial upgrade level
        if (!upgradeLevels.ContainsKey(machineId))
        {
            upgradeLevels[machineId] = currentLevel;
        }
            else if (!isUpdate)
        {
            // Only log a warning if this is a duplicate registration (not an update)
            Debug.LogWarning($"Machine {machineId} was already registered with level {upgradeLevels[machineId]}, ignoring level {currentLevel}");
        }
        
        // Notify listeners
        OnUpgradesChanged?.Invoke(upgradeLevels);
        Debug.Log($"Machine {action} for upgrades: {machineId} ({config.displayName}), Level: {upgradeLevels[machineId]}");
    }
        
        /// <summary>
        /// Get the current upgrade level for a machine
        /// </summary>
        public int GetUpgradeLevel(string machineId)
        {
            if (string.IsNullOrEmpty(machineId))
                return 0;
                
            return upgradeLevels.TryGetValue(machineId, out int level) ? level : 0;
        }
        
        /// <summary>
        /// Check if player can afford to upgrade a machine
        /// </summary>
        public bool CanAffordUpgrade(string machineId)
        {
            int price = GetUpgradePrice(machineId);
            
            // If price is -1, the machine is already at max level
            if (price < 0)
                return false;
                
            // Check if player has enough money
            return gameService != null && gameService.Money >= price;
        }
        
        /// <summary>
        /// Purchase an upgrade for a machine
        /// </summary>
        public bool PurchaseUpgrade(string machineId)
        {
            // Check if machine exists
            if (!machineConfigs.TryGetValue(machineId, out MachineConfig config))
            {
                Debug.LogError($"Cannot upgrade machine: Machine ID not found: {machineId}");
                return false;
            }
            
            // Get current level
            int currentLevel = GetUpgradeLevel(machineId);
            
            // Check if machine is already at max level
            if (currentLevel >= config.maxUpgradeLevel)
            {
                notificationService?.ShowNotification("Machine is already at maximum level!");
                return false;
            }
            
            // Get upgrade price
            int upgradePrice = GetUpgradePrice(machineId);
            
            // Check if player can afford the upgrade
            if (gameService != null && gameService.TrySpendMoney(upgradePrice))
            {
                // Apply the upgrade
                upgradeLevels[machineId] = currentLevel + 1;
                
                // Notify listeners
                OnMachineUpgraded?.Invoke(machineId, upgradeLevels[machineId]);
                OnUpgradesChanged?.Invoke(upgradeLevels);
                
                // Notify via EventBus for global event listeners
                EventBus.NotifyMachineUpgraded(machineId, upgradeLevels[machineId]);
                Debug.Log($"UpgradeService: Machine {machineId} upgraded to level {upgradeLevels[machineId]}");
                
                // Show notification
                if (notificationService != null)
                {
                string message = $"Upgraded {config.displayName} to level {upgradeLevels[machineId]}!";
                notificationService.ShowNotification(message);
                    Debug.Log($"Upgrade notification: {message}");
                }
                else
                {
                    Debug.LogError($"Cannot show upgrade notification: notificationService is null!");
            }
                
                return true;
            }
            else
            {
                // Show notification
                notificationService?.ShowNotification("Not enough money for this upgrade!");
                return false;
            }
        }
        
        /// <summary>
        /// Get the price for the next upgrade
        /// </summary>
        public int GetUpgradePrice(string machineId)
        {
            // Check if machine exists
            if (!machineConfigs.TryGetValue(machineId, out MachineConfig config))
            {
                Debug.LogError($"Cannot get upgrade price: Machine ID not found: {machineId}");
                return -1;
            }
            
            // Get current level
            int currentLevel = GetUpgradeLevel(machineId);
            
            // Check if machine is already at max level
            if (currentLevel >= config.maxUpgradeLevel)
            {
                return -1;
            }
            
            // Get upgrade price from config
            if (config.upgradeLevels != null && currentLevel < config.upgradeLevels.Length)
            {
                return config.upgradeLevels[currentLevel].upgradeCost;
            }
            
            // Default price if not specified
            return 100 * (currentLevel + 1);
        }
        
        /// <summary>
        /// Get information about the next upgrade for a machine
        /// </summary>
        public MachineUpgradeInfo GetNextUpgrade(string machineId)
        {
            // Check if machine exists
            if (!machineConfigs.TryGetValue(machineId, out MachineConfig config))
            {
                Debug.LogError($"Cannot get next upgrade: Machine ID not found: {machineId}");
                return null;
            }
            
            // Get current level
            int currentLevel = GetUpgradeLevel(machineId);
            
            // Check if machine is already at max level
            if (currentLevel >= config.maxUpgradeLevel)
            {
                return null;
            }
            
            // Create upgrade info
            MachineUpgradeInfo info = new MachineUpgradeInfo
            {
                machineId = machineId,
                machineName = config.displayName,
                currentLevel = currentLevel,
                maxLevel = config.maxUpgradeLevel
            };
            
            // Get upgrade details from config
            if (config.upgradeLevels != null && currentLevel < config.upgradeLevels.Length)
            {
                UpgradeLevelData nextLevel = config.upgradeLevels[currentLevel];
                info.nextUpgradePrice = nextLevel.upgradeCost;
                info.nextUpgradeName = nextLevel.upgradeName;
                info.nextUpgradeDescription = nextLevel.description;
                info.nextUpgradeIcon = nextLevel.upgradeIcon;
            }
            else
            {
                // Default values if not specified
                info.nextUpgradePrice = 100 * (currentLevel + 1);
                info.nextUpgradeName = $"Level {currentLevel + 1}";
                info.nextUpgradeDescription = $"Upgrade to level {currentLevel + 1}";
            }
            
            // Check if player can afford the upgrade
            info.canAfford = CanAffordUpgrade(machineId);
            
            return info;
        }
        
        /// <summary>
        /// Get information about all available upgrades
        /// </summary>
        public List<MachineUpgradeInfo> GetAllAvailableUpgrades()
        {
            List<MachineUpgradeInfo> availableUpgrades = new List<MachineUpgradeInfo>();
            
            foreach (var machineId in machineConfigs.Keys)
            {
                MachineUpgradeInfo info = GetNextUpgrade(machineId);
                if (info != null && !info.IsFullyUpgraded)
                {
                    availableUpgrades.Add(info);
                }
            }
            
            return availableUpgrades;
        }
        
        /// <summary>
        /// Get information about all machines
        /// </summary>
        public List<MachineUpgradeInfo> GetAllMachines()
        {
            List<MachineUpgradeInfo> machines = new List<MachineUpgradeInfo>();
            
            foreach (var machineId in machineConfigs.Keys)
            {
                if (!machineConfigs.TryGetValue(machineId, out MachineConfig config))
                    continue;
                    
                // Get current level
                int currentLevel = GetUpgradeLevel(machineId);
                
                // Create machine info
                MachineUpgradeInfo info = new MachineUpgradeInfo
                {
                    machineId = machineId,
                    machineName = config.displayName,
                    currentLevel = currentLevel,
                    maxLevel = config.maxUpgradeLevel
                };
                
                // Get next upgrade info if not fully upgraded
                if (currentLevel < config.maxUpgradeLevel)
                {
                    if (config.upgradeLevels != null && currentLevel < config.upgradeLevels.Length)
                    {
                        UpgradeLevelData nextLevel = config.upgradeLevels[currentLevel];
                        info.nextUpgradePrice = nextLevel.upgradeCost;
                        info.nextUpgradeName = nextLevel.upgradeName;
                        info.nextUpgradeDescription = nextLevel.description;
                        info.nextUpgradeIcon = nextLevel.upgradeIcon;
                    }
                    else
                    {
                        // Default values if not specified
                        info.nextUpgradePrice = 100 * (currentLevel + 1);
                        info.nextUpgradeName = $"Level {currentLevel + 1}";
                        info.nextUpgradeDescription = $"Upgrade to level {currentLevel + 1}";
                    }
                    
                    // Check if player can afford the upgrade
                    info.canAfford = CanAffordUpgrade(machineId);
                }
                
                machines.Add(info);
            }
            
            return machines;
        }
        public void LogRegisteredMachines()
        {
            Debug.Log($"UpgradeService has {machineConfigs.Count} registered machines:");
            foreach (var entry in machineConfigs)
            {
                Debug.Log($"- {entry.Key}: {entry.Value.displayName}, Level: {(upgradeLevels.TryGetValue(entry.Key, out int level) ? level : 0)}");
            }
        }
    }
}