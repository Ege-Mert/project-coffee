using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectCoffee.Services.Interfaces
{
    /// <summary>
    /// Interface for upgrade service to manage machine upgrades
    /// </summary>
    public interface IUpgradeService
    {
        // Events
        event Action<string, int> OnMachineUpgraded;
        event Action<Dictionary<string, int>> OnUpgradesChanged;
        
        // Properties
        Dictionary<string, int> UpgradeLevels { get; }
        
        // Methods
        int GetUpgradeLevel(string machineId);
        bool CanAffordUpgrade(string machineId);
        bool PurchaseUpgrade(string machineId);
        int GetUpgradePrice(string machineId);
        MachineUpgradeInfo GetNextUpgrade(string machineId);
        List<MachineUpgradeInfo> GetAllAvailableUpgrades();
        List<MachineUpgradeInfo> GetAllMachines();
    void RegisterMachine(string machineId, MachineConfig config, int currentLevel = 0);
    void LogRegisteredMachines();
    }
    
    /// <summary>
    /// Contains upgrade information for a machine
    /// </summary>
    [Serializable]
    public class MachineUpgradeInfo
    {
        public string machineId;
        public string machineName;
        public int currentLevel;
        public int maxLevel;
        public int nextUpgradePrice;
        public string nextUpgradeName;
        public string nextUpgradeDescription;
        public Sprite nextUpgradeIcon;
        public bool canAfford;
        
        public bool IsFullyUpgraded => currentLevel >= maxLevel;
    }
}