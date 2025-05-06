using UnityEngine;
using UnityEngine.UI;
using ProjectCoffee.Core;
using System.Collections.Generic;

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
        [SerializeField] private Text moneyText;
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

            // Subscribe to upgrade events
            UpgradeManager.Instance.OnUpgradesChanged.AddListener(RefreshUI);
            GameManager.Instance.OnMoneyChanged += OnMoneyChanged;
        }

        private void OnDestroy()
        {
            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.OnUpgradesChanged.RemoveListener(RefreshUI);
            }
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnMoneyChanged -= OnMoneyChanged;
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

            // Create upgrade items for each machine
            var machines = UpgradeManager.Instance.GetAllMachines();
            foreach (var machine in machines)
            {
                // Get next upgrade for this machine
                var nextUpgrade = UpgradeManager.Instance.GetNextUpgrade(machine.machineId);
                
                // If no next upgrade, machine is fully upgraded
                if (nextUpgrade == null)
                    continue;

                // Create UI item
                var itemGO = Instantiate(upgradeItemPrefab, upgradesContainer);
                var itemUI = itemGO.GetComponent<UpgradeItemUI>();
                
                if (itemUI != null)
                {
                    itemUI.Initialize(machine.machineId, machine.machineName, nextUpgrade, OnUpgradeClicked);
                    upgradeItems.Add(itemUI);
                }
            }
        }

        /// <summary>
        /// Handle upgrade button click
        /// </summary>
        private void OnUpgradeClicked(string machineId)
        {
            if (UpgradeManager.Instance.TryPurchaseUpgrade(machineId))
            {
                // Play purchase sound or animation
                UIManager.Instance.ShowNotification($"Upgrade purchased successfully!");
                
                // Refresh UI to show new state
                RefreshUI();
            }
            else
            {
                // Show error - not enough money
                UIManager.Instance.ShowNotification("Not enough money for this upgrade!");
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
                moneyText.text = $"Money: ${GameManager.Instance.Money}";
            }
        }
    }
}
