using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ProjectCoffee.Core
{
    /// <summary>
    /// Manages machine upgrades and progression
    /// </summary>
    public class UpgradeManager : MonoBehaviour
    {
        [System.Serializable]
        public class MachineUpgradeInfo
        {
            public string machineId;
            public string machineName;
            public GameObject machineObject;
            public int currentLevel = 0;
            public List<UpgradeData> upgrades = new List<UpgradeData>();
        }

        [System.Serializable]
        public class UpgradeData
        {
            public int level;
            public string name;
            public string description;
            public int cost;
            public Sprite icon;
        }

        private static UpgradeManager _instance;
        public static UpgradeManager Instance => _instance;

        [Header("Machine Upgrades")]
        [SerializeField] private List<MachineUpgradeInfo> machines = new List<MachineUpgradeInfo>();

        [Header("Events")]
        public UnityEvent<string, int> OnMachineUpgraded; // machineId, newLevel
        public UnityEvent OnUpgradesChanged;

        private Dictionary<string, MachineUpgradeInfo> machineDict = new Dictionary<string, MachineUpgradeInfo>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            
            // Build dictionary for fast lookup
            foreach (var machine in machines)
            {
                if (!string.IsNullOrEmpty(machine.machineId))
                {
                    machineDict[machine.machineId] = machine;
                }
            }
        }

        /// <summary>
        /// Get the current upgrade level for a machine
        /// </summary>
        public int GetUpgradeLevel(string machineId)
        {
            if (machineDict.TryGetValue(machineId, out MachineUpgradeInfo info))
            {
                return info.currentLevel;
            }
            return 0;
        }

        /// <summary>
        /// Get upgrade data for a specific machine and level
        /// </summary>
        public UpgradeData GetUpgradeData(string machineId, int level)
        {
            if (machineDict.TryGetValue(machineId, out MachineUpgradeInfo info))
            {
                return info.upgrades.Find(u => u.level == level);
            }
            return null;
        }

        /// <summary>
        /// Get the next available upgrade for a machine
        /// </summary>
        public UpgradeData GetNextUpgrade(string machineId)
        {
            if (machineDict.TryGetValue(machineId, out MachineUpgradeInfo info))
            {
                int nextLevel = info.currentLevel + 1;
                return info.upgrades.Find(u => u.level == nextLevel);
            }
            return null;
        }

        /// <summary>
        /// Check if a machine can be upgraded
        /// </summary>
        public bool CanUpgrade(string machineId)
        {
            var nextUpgrade = GetNextUpgrade(machineId);
            if (nextUpgrade == null) return false;
            
            return GameManager.Instance.Money >= nextUpgrade.cost;
        }

        /// <summary>
        /// Attempt to purchase an upgrade
        /// </summary>
        public bool TryPurchaseUpgrade(string machineId)
        {
            if (!CanUpgrade(machineId)) return false;

            var nextUpgrade = GetNextUpgrade(machineId);
            if (nextUpgrade == null) return false;

            // Spend money
            if (!GameManager.Instance.TrySpendMoney(nextUpgrade.cost))
                return false;

            // Apply upgrade
            ApplyUpgrade(machineId, nextUpgrade.level);
            return true;
        }

        /// <summary>
        /// Apply an upgrade to a machine
        /// </summary>
        private void ApplyUpgrade(string machineId, int newLevel)
        {
            if (machineDict.TryGetValue(machineId, out MachineUpgradeInfo info))
            {
                info.currentLevel = newLevel;
                
                // Notify the machine object
                if (info.machineObject != null)
                {
                    // Try different interfaces/components
                    var coffeeMachine = info.machineObject.GetComponent<ProjectCoffee.Machines.Machine<ProjectCoffee.Services.MachineService, MachineConfig>>();
                    if (coffeeMachine != null)
                    {
                        coffeeMachine.SetUpgradeLevel(newLevel);
                    }
                    else
                    {
                        // Fallback to older machine types
                        var grinder = info.machineObject.GetComponent<CoffeeGrinder>();
                        if (grinder != null) grinder.SetUpgradeLevel(newLevel);
                        
                        var grammingMachine = info.machineObject.GetComponent<CoffeeGrammingMachine>();
                        if (grammingMachine != null) grammingMachine.SetUpgradeLevel(newLevel);
                        
                        var espressoMachine = info.machineObject.GetComponent<EspressoMachine>();
                        if (espressoMachine != null) espressoMachine.SetUpgradeLevel(newLevel);
                    }
                }
                
                OnMachineUpgraded?.Invoke(machineId, newLevel);
                OnUpgradesChanged?.Invoke();
                
                Debug.Log($"Machine {info.machineName} upgraded to level {newLevel}");
            }
        }

        /// <summary>
        /// Get all available upgrades that can be purchased
        /// </summary>
        public List<(string machineId, UpgradeData upgrade)> GetAvailableUpgrades()
        {
            var availableUpgrades = new List<(string, UpgradeData)>();
            
            foreach (var machine in machines)
            {
                var nextUpgrade = GetNextUpgrade(machine.machineId);
                if (nextUpgrade != null && GameManager.Instance.Money >= nextUpgrade.cost)
                {
                    availableUpgrades.Add((machine.machineId, nextUpgrade));
                }
            }
            
            return availableUpgrades;
        }

        /// <summary>
        /// Get all machines and their upgrade status
        /// </summary>
        public List<MachineUpgradeInfo> GetAllMachines()
        {
            return new List<MachineUpgradeInfo>(machines);
        }
    }
}
