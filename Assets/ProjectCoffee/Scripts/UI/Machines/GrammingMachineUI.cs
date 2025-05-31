using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;
using ProjectCoffee.Machines;
using ProjectCoffee.Core;
using TMPro;

namespace ProjectCoffee.UI.Machines
{
    public class GrammingMachineUI : MachineUIBase<CoffeeGrammingMachine>
    {
        [Header("Gramming Machine UI")]
        [SerializeField] private Holdable grammingButton;
        [SerializeField] private Button autoDoseButton;
        [SerializeField] private TMP_Text portafilterGramText;
        [SerializeField] private TMP_Text storageGramText;
        [SerializeField] private Image qualityIndicator;
        [SerializeField] private Gradient qualityGradient;
        
        [Header("Upgrade Indicators")]
        [SerializeField] private GameObject manualGrammingIndicator;
        [SerializeField] private GameObject semiAutoIndicator;
        [SerializeField] private GameObject autoGrammingIndicator;
        
        [Header("Visual Effects")]
        [SerializeField] private Transform grammingToolSpawnPoint;
        [SerializeField] private GameObject grammingToolPrefab;
        [SerializeField] private ParticleSystem grammingParticles;
        [SerializeField] private AudioSource grammingSound;
        [SerializeField] private AudioSource autoDoseSound;
        
        private GameObject currentGrammingTool;
        private Coroutine grammingCoroutine;
        
        protected override void SetupMachineSpecificUI()
        {
            SetupGrammingButton();
            SetupAutoDoseButton();
            SubscribeToServiceEvents();
            
            UpdateStorageDisplay(Machine.StoredCoffeeAmount);
            UpdatePortafilterDisplay(Machine.PortafilterCoffeeAmount);
            UpdateUIForUpgradeLevel(Machine.GetService()?.UpgradeLevel ?? 0);
        }
        
        private void SetupGrammingButton()
        {
            if (grammingButton != null)
            {
                grammingButton.CanInteract = () => {
                    var service = Machine.GetService();
                    if (service == null) return false;
                    bool hasPortafilter = Machine.HasPortafilter;
                    bool hasCoffee = Machine.StoredCoffeeAmount > 0;
                    bool correctLevel = service.UpgradeLevel == 0;
                    return hasPortafilter && hasCoffee && correctLevel;
                };
                
                grammingButton.OnHold = OnGrammingButtonHold;
                grammingButton.OnHoldRelease = OnGrammingButtonRelease;
            }
        }
        
        private void SetupAutoDoseButton()
        {
            if (autoDoseButton != null)
            {
                autoDoseButton.onClick.RemoveAllListeners();
                autoDoseButton.onClick.AddListener(OnAutoDoseButtonClicked);
            }
        }
        
        private void SubscribeToServiceEvents()
        {
            var machineService = Machine.GetService();
            if (machineService != null)
            {
                machineService.OnCoffeeAmountChanged += UpdateStorageDisplay;
                machineService.OnPortafilterFillChanged += UpdatePortafilterDisplay;
                machineService.OnQualityEvaluated += UpdateQualityIndicator;
                machineService.OnAutoDoseCompleted += OnAutoDoseCompleted;
                machineService.OnAutoDoseStarted += OnAutoDoseStarted;
            }
        }
        
        private void Update()
        {
            UpdateAutoDoseButtonState();
            
            var service = Machine.GetService();
            if (service?.UpgradeLevel == 2 && Machine.CurrentPortafilter != null)
            {
                float serviceAmount = service.PortafilterCoffeeAmount;
                float currentAmount = Machine.CurrentPortafilter.GetItemAmount("ground_coffee");
                
                if (serviceAmount > 0 && Mathf.Abs(serviceAmount - currentAmount) > 0.01f)
                {
                    Machine.CurrentPortafilter.Clear();
                    Machine.CurrentPortafilter.TryAddItem("ground_coffee", serviceAmount);
                }
            }
        }
        
        protected override void HandleStateChanged(ProjectCoffee.Services.MachineState newState)
        {
            base.HandleStateChanged(newState);
            
            UpdateAutoDoseButtonState();
            
            if (newState == ProjectCoffee.Services.MachineState.Processing && grammingCoroutine == null)
                grammingCoroutine = StartCoroutine(GrammingVisualProcess());
            else if (newState != ProjectCoffee.Services.MachineState.Processing && grammingCoroutine != null)
            {
                StopCoroutine(grammingCoroutine);
                grammingCoroutine = null;
                CleanupGrammingTool();
            }
        }
        
        protected override void HandleUpgradeApplied(int level)
        {
            base.HandleUpgradeApplied(level);
            UpdateUIForUpgradeLevel(level);
        }
        
        private void UpdateUIForUpgradeLevel(int level)
        {
            if (grammingButton != null)
                grammingButton.gameObject.SetActive(level == 0);
                
            if (autoDoseButton != null)
                autoDoseButton.gameObject.SetActive(level == 1);
                
            if (manualGrammingIndicator != null)
                manualGrammingIndicator.SetActive(level == 0);
                
            if (semiAutoIndicator != null)
                semiAutoIndicator.SetActive(level == 1);
                
            if (autoGrammingIndicator != null)
                autoGrammingIndicator.SetActive(level == 2);
        }
        
        private void UpdateAutoDoseButtonState()
        {
            var service = Machine.GetService();
            if (autoDoseButton != null && service?.UpgradeLevel == 1)
            {
                bool hasPortafilter = Machine.HasPortafilter;
                bool hasCoffee = Machine.StoredCoffeeAmount > 0;
                bool emptyPortafilter = Machine.PortafilterCoffeeAmount <= 0;
                
                autoDoseButton.interactable = hasPortafilter && hasCoffee && emptyPortafilter;
            }
        }
        
        private void OnGrammingButtonHold(float duration)
        {
            Machine.OnGrammingButtonHold(duration);
        }
        
        private void OnGrammingButtonRelease(float heldDuration)
        {
            Machine.OnGrammingButtonRelease(heldDuration);
        }
        
        private void OnAutoDoseButtonClicked()
        {
            if (autoDoseButton != null)
                autoDoseButton.interactable = false;
                
            Machine.OnAutoDoseButtonClicked();
        }
        
        private void UpdateStorageDisplay(float amount)
        {
            if (storageGramText != null)
                storageGramText.text = $"Storage: {amount:F1}g";
        }
        
        private void UpdatePortafilterDisplay(float amount)
        {
            if (portafilterGramText != null)
            {
                if (Machine.HasPortafilter)
                    portafilterGramText.text = $"{amount:F1}g";
                else
                    portafilterGramText.text = "No Portafilter";
            }
        }
        
        private void UpdateQualityIndicator(CoffeeQualityEvaluator.QualityLevel qualityLevel)
        {
            if (qualityIndicator == null || qualityGradient == null) return;
            
            float qualityValue = 0f;
            switch (qualityLevel)
            {
                case CoffeeQualityEvaluator.QualityLevel.Poor:
                    qualityValue = 0.2f;
                    break;
                case CoffeeQualityEvaluator.QualityLevel.Acceptable:
                    qualityValue = 0.6f;
                    break;
                case CoffeeQualityEvaluator.QualityLevel.Perfect:
                    qualityValue = 1f;
                    break;
            }
            
            qualityIndicator.color = qualityGradient.Evaluate(qualityValue);
            qualityIndicator.gameObject.SetActive(true);
        }
        
        private void OnAutoDoseCompleted()
        {
            if (autoDoseSound != null)
                autoDoseSound.Play();
                
            ShowNotification("Auto-dose completed!");
            
            StartCoroutine(DelayedButtonReactivation());
        }
        
        private void OnAutoDoseStarted()
        {
            if (grammingSound != null)
                grammingSound.Play();
        }
        
        private IEnumerator DelayedButtonReactivation()
        {
            yield return new WaitForSeconds(0.5f);
            UpdateAutoDoseButtonState();
        }
        
        private IEnumerator GrammingVisualProcess()
        {
            if (grammingToolPrefab != null && grammingToolSpawnPoint != null && currentGrammingTool == null)
            {
                currentGrammingTool = Instantiate(grammingToolPrefab, grammingToolSpawnPoint);
                currentGrammingTool.transform.localPosition = Vector3.zero;
                
                currentGrammingTool.transform.localScale = Vector3.zero;
                currentGrammingTool.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            }
            
            if (grammingParticles != null)
                grammingParticles.Play();
                
            if (grammingSound != null && !grammingSound.isPlaying)
                grammingSound.Play();
            
            float pressDuration = 0.5f;
            float pressDepth = -0.1f;
            
            var service = Machine.GetService();
            while (service != null && service.CurrentState == ProjectCoffee.Services.MachineState.Processing)
            {
                if (currentGrammingTool != null)
                {
                    currentGrammingTool.transform.DOLocalMoveY(pressDepth, pressDuration).SetEase(Ease.InOutSine);
                    yield return new WaitForSeconds(pressDuration);
                    
                    currentGrammingTool.transform.DOLocalMoveY(0f, pressDuration).SetEase(Ease.InOutSine);
                    yield return new WaitForSeconds(pressDuration);
                }
                else
                {
                    yield return null;
                }
            }
            
            if (grammingParticles != null)
                grammingParticles.Stop();
                
            CleanupGrammingTool();
        }
        
        private void CleanupGrammingTool()
        {
            if (currentGrammingTool != null)
            {
                currentGrammingTool.transform.DOScale(Vector3.zero, 0.2f)
                    .OnComplete(() => 
                    {
                        Destroy(currentGrammingTool);
                        currentGrammingTool = null;
                    });
            }
        }
        
        private void ShowNotification(string message)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowNotification(message);
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            var machineService = Machine?.GetService();
            if (machineService != null)
            {
                machineService.OnCoffeeAmountChanged -= UpdateStorageDisplay;
                machineService.OnPortafilterFillChanged -= UpdatePortafilterDisplay;
                machineService.OnQualityEvaluated -= UpdateQualityIndicator;
                machineService.OnAutoDoseCompleted -= OnAutoDoseCompleted;
                machineService.OnAutoDoseStarted -= OnAutoDoseStarted;
            }
            
            if (autoDoseButton != null)
                autoDoseButton.onClick.RemoveListener(OnAutoDoseButtonClicked);
                
            if (grammingCoroutine != null)
            {
                StopCoroutine(grammingCoroutine);
                CleanupGrammingTool();
            }
        }
    }
}