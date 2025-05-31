using UnityEngine;
using UnityEngine.UI;
using ProjectCoffee.Core;
using ProjectCoffee.Services.Interfaces;
using System.Collections.Generic;
using TMPro;
using CoreServices = ProjectCoffee.Core.Services;

namespace ProjectCoffee.UI
{
    public class UpgradeUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject upgradePanel;
        [SerializeField] private Transform upgradesContainer;
        [SerializeField] private UpgradeItemUI upgradeItemPrefab;
        [SerializeField] private TMP_Text moneyText;
        [SerializeField] private Button closeButton;

        private List<UpgradeItemUI> upgradeItems = new List<UpgradeItemUI>();
        private IUpgradeService upgradeService;
        private IGameService gameService;

        private void Start()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            if (upgradePanel != null)
                upgradePanel.SetActive(false);

            EventBus.OnMoneyChanged += OnMoneyChanged;
            EventBus.OnServicesInitialized += OnServicesInitialized;
            
            TryGetServices();
        }

        private void OnDestroy()
        {
            if (upgradeService != null)
                upgradeService.OnUpgradesChanged -= HandleUpgradesChanged;
                
            EventBus.OnMoneyChanged -= OnMoneyChanged;
            EventBus.OnServicesInitialized -= OnServicesInitialized;
        }
        
        private void OnServicesInitialized()
        {
            TryGetServices();
        }
    
        private void TryGetServices()
        {
            if (upgradeService != null)
                upgradeService.OnUpgradesChanged -= HandleUpgradesChanged;
            
            upgradeService = CoreServices.Upgrade;
            gameService = CoreServices.Game;
            
            if (upgradeService != null)
            {
                upgradeService.OnUpgradesChanged += HandleUpgradesChanged;
                Debug.Log("UpgradeUI: Connected to UpgradeService");
            }
        }

        public void Show()
        {
            if (upgradePanel != null)
            {
                upgradePanel.SetActive(true);
                RefreshUI();
            }
        }

        public void Hide()
        {
            if (upgradePanel != null)
                upgradePanel.SetActive(false);
        }
        
        private void HandleUpgradesChanged(Dictionary<string, int> upgradeLevels)
        {
            RefreshUI();
        }

        private void RefreshUI()
        {
            foreach (var item in upgradeItems)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            upgradeItems.Clear();

            UpdateMoneyDisplay();

            if (upgradeService == null)
            {
                Debug.LogWarning("UpgradeUI: Service not available");
                return;
            }
            
            var machines = upgradeService.GetAllMachines();
            foreach (var machine in machines)
            {
                if (machine.IsFullyUpgraded)
                    continue;

                var itemGO = Instantiate(upgradeItemPrefab, upgradesContainer);
                var itemUI = itemGO.GetComponent<UpgradeItemUI>();
                
                if (itemUI != null)
                {
                    itemUI.Initialize(machine.machineId, machine.machineName, machine, OnUpgradeClicked);
                    upgradeItems.Add(itemUI);
                }
            }
        }

        private void OnUpgradeClicked(string machineId)
        {
            if (upgradeService == null)
            {
                Debug.LogError("UpgradeUI: Service not available");
                return;
            }
            
            bool success = upgradeService.PurchaseUpgrade(machineId);
            
            if (success)
                RefreshUI();
        }

        private void OnMoneyChanged(int newAmount)
        {
            UpdateMoneyDisplay();
            
            foreach (var item in upgradeItems)
            {
                if (item != null)
                    item.UpdateAffordability();
            }
        }

        private void UpdateMoneyDisplay()
        {
            if (moneyText != null)
            {
                int currentMoney = CoreServices.Game?.Money ?? 0;
                moneyText.text = $"Money: ${currentMoney}";
            }
        }
    }
}