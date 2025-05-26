using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectCoffee.Core;
using ProjectCoffee.Core.Services;
using ProjectCoffee.Services.Interfaces;

namespace ProjectCoffee.Services
{
    public class UpgradeService : IUpgradeService
    {
        public event Action<string, int> OnMachineUpgraded;
        public event Action<Dictionary<string, int>> OnUpgradesChanged;
        
        private readonly Dictionary<string, int> upgradeLevels = new Dictionary<string, int>();
        private readonly Dictionary<string, MachineConfig> machineConfigs = new Dictionary<string, MachineConfig>();
        
        private IGameService gameService;
        private INotificationService notificationService;
        
        public Dictionary<string, int> UpgradeLevels => upgradeLevels;
        
        public UpgradeService()
        {
            gameService = ServiceLocator.Instance.GetService<IGameService>();
            notificationService = ServiceLocator.Instance.GetService<INotificationService>();
            
            EventBus.OnMachineRegistered += RegisterMachine;
            Debug.Log("UpgradeService initialized");
        }
        
        public void RegisterMachine(string machineId, MachineConfig config, int currentLevel = 0)
        {
            if (string.IsNullOrEmpty(machineId) || config == null) return;
            
            machineConfigs[machineId] = config;
            
            if (!upgradeLevels.ContainsKey(machineId))
            {
                upgradeLevels[machineId] = currentLevel;
            }
            
            OnUpgradesChanged?.Invoke(upgradeLevels);
            Debug.Log($"Machine registered: {machineId} ({config.displayName}), Level: {upgradeLevels[machineId]}");
        }
        
        public int GetUpgradeLevel(string machineId)
        {
            return upgradeLevels.TryGetValue(machineId, out int level) ? level : 0;
        }
        
        public bool CanAffordUpgrade(string machineId)
        {
            int price = GetUpgradePrice(machineId);
            return price >= 0 && gameService != null && gameService.Money >= price;
        }
        
        public bool PurchaseUpgrade(string machineId)
        {
            if (!machineConfigs.TryGetValue(machineId, out MachineConfig config))
                return false;
            
            int currentLevel = GetUpgradeLevel(machineId);
            
            if (currentLevel >= config.maxUpgradeLevel)
            {
                notificationService?.ShowNotification("Machine is already at maximum level!");
                return false;
            }
            
            int upgradePrice = GetUpgradePrice(machineId);
            
            if (gameService != null && gameService.TrySpendMoney(upgradePrice))
            {
                upgradeLevels[machineId] = currentLevel + 1;
                
                OnMachineUpgraded?.Invoke(machineId, upgradeLevels[machineId]);
                OnUpgradesChanged?.Invoke(upgradeLevels);
                EventBus.NotifyMachineUpgraded(machineId, upgradeLevels[machineId]);
                
                notificationService?.ShowNotification($"Upgraded {config.displayName} to level {upgradeLevels[machineId]}!");
                return true;
            }
            
            notificationService?.ShowNotification("Not enough money for this upgrade!");
            return false;
        }
        
        public int GetUpgradePrice(string machineId)
        {
            if (!machineConfigs.TryGetValue(machineId, out MachineConfig config))
                return -1;
            
            int currentLevel = GetUpgradeLevel(machineId);
            
            if (currentLevel >= config.maxUpgradeLevel)
                return -1;
            
            if (config.upgradeLevels != null && currentLevel < config.upgradeLevels.Length)
                return config.upgradeLevels[currentLevel].upgradeCost;
            
            return 100 * (currentLevel + 1);
        }
        
        public MachineUpgradeInfo GetNextUpgrade(string machineId)
        {
            if (!machineConfigs.TryGetValue(machineId, out MachineConfig config))
                return null;
            
            int currentLevel = GetUpgradeLevel(machineId);
            
            if (currentLevel >= config.maxUpgradeLevel)
                return null;
            
            var info = new MachineUpgradeInfo
            {
                machineId = machineId,
                machineName = config.displayName,
                currentLevel = currentLevel,
                maxLevel = config.maxUpgradeLevel,
                canAfford = CanAffordUpgrade(machineId)
            };
            
            if (config.upgradeLevels != null && currentLevel < config.upgradeLevels.Length)
            {
                var nextLevel = config.upgradeLevels[currentLevel];
                info.nextUpgradePrice = nextLevel.upgradeCost;
                info.nextUpgradeName = nextLevel.upgradeName;
                info.nextUpgradeDescription = nextLevel.description;
                info.nextUpgradeIcon = nextLevel.upgradeIcon;
            }
            else
            {
                info.nextUpgradePrice = 100 * (currentLevel + 1);
                info.nextUpgradeName = $"Level {currentLevel + 1}";
                info.nextUpgradeDescription = $"Upgrade to level {currentLevel + 1}";
            }
            
            return info;
        }
        
        public List<MachineUpgradeInfo> GetAllAvailableUpgrades()
        {
            var availableUpgrades = new List<MachineUpgradeInfo>();
            
            foreach (var machineId in machineConfigs.Keys)
            {
                var info = GetNextUpgrade(machineId);
                if (info != null && !info.IsFullyUpgraded)
                {
                    availableUpgrades.Add(info);
                }
            }
            
            return availableUpgrades;
        }
        
        public List<MachineUpgradeInfo> GetAllMachines()
        {
            var machines = new List<MachineUpgradeInfo>();
            
            foreach (var kvp in machineConfigs)
            {
                var machineId = kvp.Key;
                var config = kvp.Value;
                int currentLevel = GetUpgradeLevel(machineId);
                
                var info = new MachineUpgradeInfo
                {
                    machineId = machineId,
                    machineName = config.displayName,
                    currentLevel = currentLevel,
                    maxLevel = config.maxUpgradeLevel
                };
                
                if (currentLevel < config.maxUpgradeLevel)
                {
                    var nextUpgrade = GetNextUpgrade(machineId);
                    if (nextUpgrade != null)
                    {
                        info.nextUpgradePrice = nextUpgrade.nextUpgradePrice;
                        info.nextUpgradeName = nextUpgrade.nextUpgradeName;
                        info.nextUpgradeDescription = nextUpgrade.nextUpgradeDescription;
                        info.nextUpgradeIcon = nextUpgrade.nextUpgradeIcon;
                        info.canAfford = nextUpgrade.canAfford;
                    }
                }
                
                machines.Add(info);
            }
            
            return machines;
        }

        public void LogRegisteredMachines()
        {
            throw new NotImplementedException();
        }

    }
}