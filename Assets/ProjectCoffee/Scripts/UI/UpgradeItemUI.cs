using UnityEngine;
using UnityEngine.UI;
using ProjectCoffee.Core;
using ProjectCoffee.Services.Interfaces;
using System;
using TMPro;
using CoreServices = ProjectCoffee.Core.Services;

namespace ProjectCoffee.UI
{
    public class UpgradeItemUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text machineNameText;
        [SerializeField] private TMP_Text upgradeNameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private TMP_Text upgradeButtonText;

        private string machineId;
        private MachineUpgradeInfo upgradeInfo;
        private Action<string> onUpgradeClicked;

        public void Initialize(string machineId, string machineName, MachineUpgradeInfo upgrade, Action<string> clickCallback)
        {
            this.machineId = machineId;
            this.upgradeInfo = upgrade;
            this.onUpgradeClicked = clickCallback;

            if (machineNameText != null)
                machineNameText.text = machineName;

            if (upgradeNameText != null)
                upgradeNameText.text = upgrade.nextUpgradeName;

            if (descriptionText != null)
                descriptionText.text = upgrade.nextUpgradeDescription;

            if (costText != null)
                costText.text = $"${upgrade.nextUpgradePrice}";

            if (iconImage != null && upgrade.nextUpgradeIcon != null)
                iconImage.sprite = upgrade.nextUpgradeIcon;
            else if (iconImage != null)
                iconImage.gameObject.SetActive(false);

            if (upgradeButton != null)
                upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);

            UpdateAffordability();
        }

        public void UpdateAffordability()
        {
            bool canAfford = false;
            
            var upgradeService = CoreServices.Upgrade;
            if (upgradeService != null)
            {
                canAfford = upgradeService.CanAffordUpgrade(machineId);
            }
            else if (GameManager.Instance != null)
            {
                int price = upgradeInfo?.nextUpgradePrice ?? 9999;
                canAfford = GameManager.Instance.Money >= price;
            }
            
            if (upgradeButton != null)
                upgradeButton.interactable = canAfford;

            if (upgradeButtonText != null)
            {
                upgradeButtonText.text = canAfford ? "UPGRADE" : "NOT ENOUGH $";
                upgradeButtonText.color = canAfford ? Color.white : new Color(1, 0.7f, 0.7f);
            }

            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                canvasGroup.alpha = canAfford ? 1f : 0.7f;
        }

        private void OnUpgradeButtonClicked()
        {
            onUpgradeClicked?.Invoke(machineId);
        }

        private void OnDestroy()
        {
            if (upgradeButton != null)
                upgradeButton.onClick.RemoveListener(OnUpgradeButtonClicked);
        }
    }
}