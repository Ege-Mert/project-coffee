using UnityEngine;
using UnityEngine.UI;
using ProjectCoffee.Core;
using System;

namespace ProjectCoffee.UI
{
    /// <summary>
    /// UI component for displaying a single upgrade option
    /// </summary>
    public class UpgradeItemUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Text machineNameText;
        [SerializeField] private Text upgradeNameText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text costText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Text upgradeButtonText;

        private string machineId;
        private UpgradeManager.UpgradeData upgradeData;
        private Action<string> onUpgradeClicked;

        /// <summary>
        /// Initialize this upgrade item
        /// </summary>
        public void Initialize(string machineId, string machineName, UpgradeManager.UpgradeData upgrade, Action<string> clickCallback)
        {
            this.machineId = machineId;
            this.upgradeData = upgrade;
            this.onUpgradeClicked = clickCallback;

            // Set UI elements
            if (machineNameText != null)
                machineNameText.text = machineName;

            if (upgradeNameText != null)
                upgradeNameText.text = upgrade.name;

            if (descriptionText != null)
                descriptionText.text = upgrade.description;

            if (costText != null)
                costText.text = $"${upgrade.cost}";

            if (iconImage != null && upgrade.icon != null)
                iconImage.sprite = upgrade.icon;

            if (upgradeButton != null)
            {
                upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
            }

            UpdateAffordability();
        }

        /// <summary>
        /// Update the button state based on affordability
        /// </summary>
        public void UpdateAffordability()
        {
            bool canAfford = GameManager.Instance.Money >= upgradeData.cost;
            
            if (upgradeButton != null)
            {
                upgradeButton.interactable = canAfford;
            }

            if (upgradeButtonText != null)
            {
                upgradeButtonText.text = canAfford ? "UPGRADE" : "NOT ENOUGH $";
                upgradeButtonText.color = canAfford ? Color.white : new Color(1, 0.7f, 0.7f);
            }

            // Visual feedback for affordability
            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = canAfford ? 1f : 0.7f;
            }
        }

        /// <summary>
        /// Handle upgrade button click
        /// </summary>
        private void OnUpgradeButtonClicked()
        {
            onUpgradeClicked?.Invoke(machineId);
        }

        private void OnDestroy()
        {
            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveListener(OnUpgradeButtonClicked);
            }
        }
    }
}
