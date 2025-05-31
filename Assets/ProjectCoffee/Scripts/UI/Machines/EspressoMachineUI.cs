using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using ProjectCoffee.Machines;
using ProjectCoffee.Core;
using ProjectCoffee.Services;
using ProjectCoffee.Services.Interfaces;
using ProjectCoffee.Machines.Components;
using CoreServices = ProjectCoffee.Core.Services; // Add this alias

namespace ProjectCoffee.UI.Machines
{
    public class EspressoMachineUI : MachineUIBase<EspressoMachine>
    {
        [System.Serializable]
        public class BrewingSlotUI
        {
            public EspressoMachineDropZone portafilterZone;
            public EspressoMachineDropZone cupZone;
            public GameObject activeIndicator;
            public GameObject readyIndicator;
            public Image progressFill;
            public ParticleSystem brewingParticles;
            public Transform slotContainer;
            
            [HideInInspector] public Coroutine brewingCoroutine;
        }
        
        [Header("Espresso Machine UI")]
        [SerializeField] private List<BrewingSlotUI> brewingSlotUIs = new List<BrewingSlotUI>();
        [SerializeField] private BrewButton brewButton;
        [SerializeField] private GameObject autoBrewIndicator;
        [SerializeField] private GameObject manualBrewIndicator;
        
        [Header("Brewing Effects")]
        [SerializeField] private AudioSource brewingSound;
        [SerializeField] private AudioSource brewCompleteSound;
        [SerializeField] private float brewingShakeIntensity = 0.01f;
        
        private Dictionary<int, Coroutine> brewingCoroutines = new Dictionary<int, Coroutine>();
        
        protected override void SetupMachineSpecificUI()
        {
            SetupBrewButton();
            InitializeSlotUIs();
            SubscribeToServiceEvents();
            UpdateUIForUpgradeLevel(Machine.GetService()?.UpgradeLevel ?? 0);
        }
        
        private void SetupBrewButton()
        {
            if (brewButton != null)
            {
                brewButton.CanInteractCustomCheck = () => Machine.CanBrewAnySlot();
                brewButton.OnClicked.AddListener(OnBrewButtonClicked);
            }
        }
        
        private void InitializeSlotUIs()
        {
            for (int i = 0; i < brewingSlotUIs.Count; i++)
            {
                var slotUI = brewingSlotUIs[i];
                
                if (slotUI.activeIndicator != null)
                    slotUI.activeIndicator.SetActive(false);
                    
                if (slotUI.readyIndicator != null)
                    slotUI.readyIndicator.SetActive(false);
                    
                if (slotUI.progressFill != null)
                    slotUI.progressFill.fillAmount = 0f;
                
                if (slotUI.portafilterZone != null)
                    slotUI.portafilterZone.SlotIndex = i;
                    
                if (slotUI.cupZone != null)
                    slotUI.cupZone.SlotIndex = i;
            }
        }
        
        private void SubscribeToServiceEvents()
        {
            var machineService = Machine.GetService();
            if (machineService != null)
            {
                machineService.OnSlotStateChanged += HandleSlotStateChanged;
                machineService.OnSlotProgressChanged += HandleSlotProgressChanged;
                machineService.OnBrewingCompleted += HandleBrewingCompleted;
            }
        }
        
        protected override void HandleStateChanged(MachineState newState)
        {
            base.HandleStateChanged(newState);
            UpdateBrewButtonState();
        }
        
        protected override void HandleUpgradeApplied(int level)
        {
            base.HandleUpgradeApplied(level);
            UpdateUIForUpgradeLevel(level);
        }
        
        private void UpdateUIForUpgradeLevel(int level)
        {
            if (brewButton != null)
                brewButton.gameObject.SetActive(level < 2);
                
            if (autoBrewIndicator != null)
                autoBrewIndicator.SetActive(level == 2);
                
            if (manualBrewIndicator != null)
                manualBrewIndicator.SetActive(level < 2);
            
            for (int i = 0; i < brewingSlotUIs.Count; i++)
            {
                bool shouldBeActive = i < 2 || level >= 2;
                
                if (brewingSlotUIs[i].slotContainer != null)
                    brewingSlotUIs[i].slotContainer.gameObject.SetActive(shouldBeActive);
            }
        }
        
        private void OnBrewButtonClicked()
        {
            Machine.OnBrewButtonClicked();
        }
        
        private void UpdateBrewButtonState()
        {
            var machineService = Machine.GetService();
            if (brewButton != null && machineService != null && machineService.UpgradeLevel < 2)
            {
                brewButton.gameObject.SetActive(true);
                bool canBrew = Machine.CanBrewAnySlot();
                
                var buttonComponent = brewButton.GetComponent<Button>();
                if (buttonComponent != null)
                    buttonComponent.interactable = canBrew;
            }
        }
        
        private void HandleSlotStateChanged(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= brewingSlotUIs.Count) return;
            
            var slotUI = brewingSlotUIs[slotIndex];
            var espressoService = CoreServices.Get<IEspressoMachineService>(); // Use alias
            var slot = espressoService?.GetSlot(slotIndex);
            
            if (slot == null) return;
            
            if (slotUI.activeIndicator != null)
                slotUI.activeIndicator.SetActive(slot.isActive);
                
            if (slotUI.readyIndicator != null)
            {
                bool isReady = slot.hasPortafilter && slot.hasCup && slot.hasGroundCoffee && !slot.isActive;
                slotUI.readyIndicator.SetActive(isReady);
            }
            
            UpdateSlotItemStates(slotIndex, slot.isActive);
            
            if (slot.isActive && !brewingCoroutines.ContainsKey(slotIndex))
            {
                brewingCoroutines[slotIndex] = StartCoroutine(BrewingVisualEffects(slotIndex));
            }
            else if (!slot.isActive && brewingCoroutines.ContainsKey(slotIndex))
            {
                var coroutine = brewingCoroutines[slotIndex];
                if (coroutine != null)
                    StopCoroutine(coroutine);
                brewingCoroutines.Remove(slotIndex);
                StopBrewingEffects(slotIndex);
            }
            
            UpdateBrewButtonState();
        }
        
        private void UpdateSlotItemStates(int slotIndex, bool isProcessing)
        {
            var currentPortafilters = Machine.GetCurrentPortafilters();
            var currentCups = Machine.GetCurrentCups();
            
            if (currentPortafilters.ContainsKey(slotIndex))
            {
                var portafilterDraggable = currentPortafilters[slotIndex].GetComponent<Draggable>();
                if (portafilterDraggable != null)
                    portafilterDraggable.enabled = !isProcessing;
            }
            
            if (currentCups.ContainsKey(slotIndex))
            {
                var cupDraggable = currentCups[slotIndex].GetComponent<Draggable>();
                if (cupDraggable != null)
                    cupDraggable.enabled = !isProcessing;
            }
        }
        
        private void HandleSlotProgressChanged(int slotIndex, float progress)
        {
            if (slotIndex < 0 || slotIndex >= brewingSlotUIs.Count) return;
            
            var slotUI = brewingSlotUIs[slotIndex];
            
            if (slotUI.progressFill != null)
                slotUI.progressFill.fillAmount = progress;
        }
        
        private void HandleBrewingCompleted(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= brewingSlotUIs.Count) return;
            
            var slotUI = brewingSlotUIs[slotIndex];
            
            StopBrewingEffects(slotIndex);
            
            if (brewCompleteSound != null)
                brewCompleteSound.Play();
            
            if (slotUI.progressFill != null)
                slotUI.progressFill.DOFillAmount(0f, 0.3f);
            
            if (slotUI.slotContainer != null)
                slotUI.slotContainer.DOPunchScale(Vector3.one * 0.1f, 0.3f, 2);
            
            var espressoService = CoreServices.Get<IEspressoMachineService>(); // Use alias
            var slot = espressoService?.GetSlot(slotIndex);
            
            if (slot != null)
            {
                string qualityDescription = GetQualityDescription(slot.coffeeQuality);
                ShowNotification($"{qualityDescription} espresso ready!");
            }
        }
        
        private IEnumerator BrewingVisualEffects(int slotIndex)
        {
            var slotUI = brewingSlotUIs[slotIndex];
            
            if (slotUI.brewingParticles != null)
                slotUI.brewingParticles.Play();
            
            if (brewingSound != null && !brewingSound.isPlaying)
                brewingSound.Play();
            
            Transform machineTransform = slotUI.slotContainer ?? transform;
            if (machineTransform == null) yield break;
            
            Vector3 originalPosition = machineTransform.localPosition;
            
            var espressoService = CoreServices.Get<IEspressoMachineService>(); // Use alias
            
            while (espressoService?.GetSlot(slotIndex)?.isActive ?? false)
            {
                if (machineTransform != null)
                    machineTransform.localPosition = originalPosition + (Vector3)Random.insideUnitCircle * brewingShakeIntensity;
                
                if (slotUI.progressFill != null)
                {
                    slotUI.progressFill.transform.DOScale(Vector3.one * 1.02f, 0.5f)
                        .SetLoops(2, LoopType.Yoyo)
                        .SetEase(Ease.InOutSine);
                }
                
                yield return new WaitForSeconds(0.05f);
            }
            
            if (machineTransform != null)
                machineTransform.localPosition = originalPosition;
        }
        
        private void StopBrewingEffects(int slotIndex)
        {
            var slotUI = brewingSlotUIs[slotIndex];
            
            if (slotUI.brewingParticles != null)
                slotUI.brewingParticles.Stop();
            
            bool anySlotBrewing = false;
            var espressoService = CoreServices.Get<IEspressoMachineService>(); // Use alias
            
            for (int i = 0; i < Machine.GetSlotCount(); i++)
            {
                if (espressoService?.GetSlot(i)?.isActive ?? false)
                {
                    anySlotBrewing = true;
                    break;
                }
            }
            
            if (!anySlotBrewing && brewingSound != null && brewingSound.isPlaying)
                brewingSound.Stop();
        }
        
        private string GetQualityDescription(float qualityFactor)
        {
            if (qualityFactor >= 0.9f) return "Perfect";
            else if (qualityFactor >= 0.7f) return "Excellent";
            else if (qualityFactor >= 0.5f) return "Good";
            else if (qualityFactor >= 0.3f) return "Okay";
            else return "Poor";
        }
        
        private void ShowNotification(string message)
        {
            CoreServices.UI?.ShowNotification(message); // Use alias
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            var machineService = Machine?.GetService();
            if (machineService != null)
            {
                machineService.OnSlotStateChanged -= HandleSlotStateChanged;
                machineService.OnSlotProgressChanged -= HandleSlotProgressChanged;
                machineService.OnBrewingCompleted -= HandleBrewingCompleted;
            }
            
            if (brewButton != null)
                brewButton.OnClicked.RemoveListener(OnBrewButtonClicked);
            
            foreach (var kvp in brewingCoroutines)
            {
                if (kvp.Value != null)
                    StopCoroutine(kvp.Value);
            }
        }
    }
}
