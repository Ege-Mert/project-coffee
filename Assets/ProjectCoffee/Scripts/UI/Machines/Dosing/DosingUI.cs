using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;
using ProjectCoffee.Machines.Dosing;
using ProjectCoffee.Services.Dosing;
using ProjectCoffee.Services;
using ProjectCoffee.Core;
using ProjectCoffee.Machines.Dosing.Logic;
using TMPro;

namespace ProjectCoffee.UI.Machines.Dosing
{
    /// <summary>
    /// Clean dosing UI focused purely on visual representation and user interaction.
    /// All business logic delegated to service, all state updates event-driven.
    /// </summary>
    public class DosingUI : MachineUIBase<DosingMachine>
    {
        [Header("Dosing Machine UI")]
        [SerializeField] private Holdable dosingButton;
        [SerializeField] private Button autoDoseButton;
        [SerializeField] private TMP_Text portafilterGramText;
        [SerializeField] private TMP_Text storageGramText;
        [SerializeField] private Image qualityIndicator;
        [SerializeField] private Gradient qualityGradient;
        
        [Header("Upgrade Indicators")]
        [SerializeField] private GameObject manualDosingIndicator;
        [SerializeField] private GameObject semiAutoIndicator;
        [SerializeField] private GameObject autoDosingIndicator;
        
        [Header("Visual Effects")]
        [SerializeField] private Transform dosingToolSpawnPoint;
        [SerializeField] private GameObject dosingToolPrefab;
        [SerializeField] private ParticleSystem dosingParticles;
        [SerializeField] private AudioSource dosingSound;
        [SerializeField] private AudioSource autoDoseSound;
        
        private DosingService service;
        private GameObject currentDosingTool;
        private Coroutine dosingEffectCoroutine;
        
        #region Initialization
        
        protected override void SetupMachineSpecificUI()
        {
            GetServiceReference();
            SetupInteractionElements();
            SubscribeToServiceEvents();
            
            // Initialize display
            if (service != null)
            {
                UpdateStorageDisplay(service.StoredCoffeeAmount);
                UpdatePortafilterDisplay(service.PortafilterCoffeeAmount);
                UpdateUIForUpgradeLevel(service.UpgradeLevel);
            }
        }
        
        private void GetServiceReference()
        {
            service = Machine?.GetService();
            if (service == null)
            {
                Debug.LogError("DosingUI: Failed to get service reference!");
            }
        }
        
        private void SetupInteractionElements()
        {
            SetupDosingButton();
            SetupAutoDoseButton();
        }
        
        #endregion
        
        #region Button Setup
        
        private void SetupDosingButton()
        {
            if (dosingButton == null) return;
            
            dosingButton.CanInteract = () => CanUseManualDosing();
            dosingButton.OnHold = OnDosingButtonHold;
            dosingButton.OnHoldRelease = OnDosingButtonRelease;
        }
        
        private void SetupAutoDoseButton()
        {
            if (autoDoseButton == null) return;
            
            autoDoseButton.onClick.RemoveAllListeners();
            autoDoseButton.onClick.AddListener(OnAutoDoseButtonClicked);
        }
        
        private bool CanUseManualDosing()
        {
            return service != null && 
                   service.UpgradeLevel == 0 && 
                   service.HasPortafilter && 
                   service.StoredCoffeeAmount > 0;
        }
        
        #endregion
        
        #region Event Subscriptions
        
        private void SubscribeToServiceEvents()
        {
            if (service != null)
            {
                service.OnCoffeeAmountChanged += UpdateStorageDisplay;
                service.OnPortafilterFillChanged += UpdatePortafilterDisplay;
                service.OnQualityEvaluated += UpdateQualityIndicator;
                service.OnAutoDoseStarted += OnAutoDoseStarted;
                service.OnAutoDoseCompleted += OnAutoDoseCompleted;
                service.OnUpgradeApplied += HandleUpgradeApplied;
                service.OnPortafilterPresenceChanged += OnPortafilterPresenceChanged;
            }
        }
        
        private void UnsubscribeFromServiceEvents()
        {
            if (service != null)
            {
                service.OnCoffeeAmountChanged -= UpdateStorageDisplay;
                service.OnPortafilterFillChanged -= UpdatePortafilterDisplay;
                service.OnQualityEvaluated -= UpdateQualityIndicator;
                service.OnAutoDoseStarted -= OnAutoDoseStarted;
                service.OnAutoDoseCompleted -= OnAutoDoseCompleted;
                service.OnUpgradeApplied -= HandleUpgradeApplied;
                service.OnPortafilterPresenceChanged -= OnPortafilterPresenceChanged;
            }
        }
        
        #endregion
        
        #region User Interaction Handlers
        
        private void OnDosingButtonHold(float duration)
        {
            Machine?.OnDosingButtonHold(duration);
        }
        
        private void OnDosingButtonRelease(float heldDuration)
        {
            Machine?.OnDosingButtonRelease(heldDuration);
        }
        
        private void OnAutoDoseButtonClicked()
        {
            if (autoDoseButton != null)
            {
                autoDoseButton.interactable = false;
            }
            
            Machine?.OnAutoDoseButtonClicked();
        }
        
        #endregion
        
        #region Visual Updates
        
        private void UpdateStorageDisplay(float amount)
        {
            if (storageGramText != null)
            {
                storageGramText.text = $"Storage: {amount:F1}g";
            }
        }
        
        private void UpdatePortafilterDisplay(float amount)
        {
            if (portafilterGramText != null)
            {
                if (service != null && service.HasPortafilter)
                {
                    portafilterGramText.text = $"{amount:F1}g";
                }
                else
                {
                    portafilterGramText.text = "No Portafilter";
                }
            }
            
            // Update button state when portafilter amount changes
            UpdateAutoDoseButtonState();
        }
        
        private void UpdateQualityIndicator(QualityResult qualityResult)
        {
            if (qualityIndicator == null || qualityGradient == null) return;
            
            float qualityValue = qualityResult.Level switch
            {
                CoffeeQualityEvaluator.QualityLevel.Poor => 0.2f,
                CoffeeQualityEvaluator.QualityLevel.Acceptable => 0.6f,
                CoffeeQualityEvaluator.QualityLevel.Perfect => 1f,
                _ => 0.2f
            };
            
            qualityIndicator.color = qualityGradient.Evaluate(qualityValue);
            qualityIndicator.gameObject.SetActive(true);
            
            // Animate quality indicator
            qualityIndicator.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 2, 0.5f);
        }
        
        #endregion
        
        #region State Change Handling
        
        protected override void HandleStateChanged(MachineState newState)
        {
            base.HandleStateChanged(newState);
            
            UpdateInteractionStates();
            HandleProcessingVisuals(newState);
        }
        
        private void UpdateInteractionStates()
        {
            UpdateAutoDoseButtonState();
            // Manual button updates through CanInteract delegate
        }
        
        private void UpdateAutoDoseButtonState()
        {
            if (autoDoseButton != null && service != null && service.UpgradeLevel == 1)
            {
                // For level 1 (semi-auto): Button should be enabled when:
                // 1. Has portafilter
                // 2. Has coffee in storage  
                // 3. Machine is not currently processing
                // 4. Portafilter is empty (for fresh dose)
                bool hasPortafilter = service.HasPortafilter;
                bool hasStorage = service.StoredCoffeeAmount > 0;
                bool notProcessing = service.CurrentState != MachineState.Processing;
                bool isEmpty = service.PortafilterCoffeeAmount == 0;
                
                bool canUse = hasPortafilter && hasStorage && notProcessing && isEmpty;
                
                autoDoseButton.interactable = canUse;
                
                Debug.Log($"DosingUI: Button state - Portafilter: {hasPortafilter}, " +
                         $"Storage: {hasStorage}, NotProcessing: {notProcessing}, " +
                         $"IsEmpty: {isEmpty} ({service.PortafilterCoffeeAmount:F1}g), " +
                         $"CanUse: {canUse}");
            }
        }
        
        private void HandleProcessingVisuals(MachineState state)
        {
            if (state == MachineState.Processing && dosingEffectCoroutine == null)
            {
                dosingEffectCoroutine = StartCoroutine(DosingVisualEffect());
            }
            else if (state != MachineState.Processing && dosingEffectCoroutine != null)
            {
                StopCoroutine(dosingEffectCoroutine);
                dosingEffectCoroutine = null;
                CleanupDosingTool();
            }
        }
        
        #endregion
        
        #region Upgrade Handling
        
        protected override void HandleUpgradeApplied(int level)
        {
            base.HandleUpgradeApplied(level);
            UpdateUIForUpgradeLevel(level);
        }
        
        private void UpdateUIForUpgradeLevel(int level)
        {
            UpdateControlsVisibility(level);
            UpdateUpgradeIndicators(level);
        }
        
        private void UpdateControlsVisibility(int level)
        {
            if (dosingButton != null)
                dosingButton.gameObject.SetActive(level == 0);
                
            if (autoDoseButton != null)
                autoDoseButton.gameObject.SetActive(level == 1);
        }
        
        private void UpdateUpgradeIndicators(int level)
        {
            if (manualDosingIndicator != null)
                manualDosingIndicator.SetActive(level == 0);
                
            if (semiAutoIndicator != null)
                semiAutoIndicator.SetActive(level == 1);
                
            if (autoDosingIndicator != null)
                autoDosingIndicator.SetActive(level == 2);
        }
        
        #endregion
        
        #region Auto-Dose Event Handlers
        
        private void OnAutoDoseStarted()
        {
            if (dosingSound != null)
            {
                dosingSound.Play();
            }
            
            ShowNotification("Auto-dosing started...");
        }
        
        private void OnAutoDoseCompleted()
        {
            if (autoDoseSound != null)
            {
                autoDoseSound.Play();
            }
            
            ShowNotification("Auto-dose completed!");
            StartCoroutine(DelayedButtonReactivation());
        }
        
        private void OnPortafilterPresenceChanged(bool hasPortafilter)
        {
            Debug.Log($"DosingUI: Portafilter presence changed to {hasPortafilter}");
            UpdateAutoDoseButtonState();
        }
        
        private IEnumerator DelayedButtonReactivation()
        {
            yield return new WaitForSeconds(0.5f);
            UpdateAutoDoseButtonState();
        }
        
        #endregion
        
        #region Visual Effects
        
        private IEnumerator DosingVisualEffect()
        {
            CreateDosingTool();
            StartParticleEffects();
            PlayDosingSound();
            
            yield return StartCoroutine(AnimateDosingTool());
            
            StopParticleEffects();
            CleanupDosingTool();
        }
        
        private void CreateDosingTool()
        {
            if (dosingToolPrefab != null && dosingToolSpawnPoint != null && currentDosingTool == null)
            {
                currentDosingTool = Instantiate(dosingToolPrefab, dosingToolSpawnPoint);
                currentDosingTool.transform.localPosition = Vector3.zero;
                
                // Animate tool appearance
                currentDosingTool.transform.localScale = Vector3.zero;
                currentDosingTool.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            }
        }
        
        private IEnumerator AnimateDosingTool()
        {
            if (currentDosingTool == null) yield break;
            
            float pressDuration = 0.5f;
            float pressDepth = -0.1f;
            
            while (service != null && service.CurrentState == MachineState.Processing)
            {
                // Press down
                currentDosingTool.transform.DOLocalMoveY(pressDepth, pressDuration).SetEase(Ease.InOutSine);
                yield return new WaitForSeconds(pressDuration);
                
                // Press up
                currentDosingTool.transform.DOLocalMoveY(0f, pressDuration).SetEase(Ease.InOutSine);
                yield return new WaitForSeconds(pressDuration);
            }
        }
        
        private void StartParticleEffects()
        {
            if (dosingParticles != null)
            {
                dosingParticles.Play();
            }
        }
        
        private void StopParticleEffects()
        {
            if (dosingParticles != null)
            {
                dosingParticles.Stop();
            }
        }
        
        private void PlayDosingSound()
        {
            if (dosingSound != null && !dosingSound.isPlaying)
            {
                dosingSound.Play();
            }
        }
        
        private void CleanupDosingTool()
        {
            if (currentDosingTool != null)
            {
                currentDosingTool.transform.DOScale(Vector3.zero, 0.2f)
                    .OnComplete(() => 
                    {
                        Destroy(currentDosingTool);
                        currentDosingTool = null;
                    });
            }
        }
        
        #endregion
        
        #region Utility
        
        private void ShowNotification(string message)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowNotification(message);
            }
        }
        
        #endregion
        
        #region Cleanup
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            UnsubscribeFromServiceEvents();
            
            if (autoDoseButton != null)
            {
                autoDoseButton.onClick.RemoveListener(OnAutoDoseButtonClicked);
            }
            
            if (dosingEffectCoroutine != null)
            {
                StopCoroutine(dosingEffectCoroutine);
                CleanupDosingTool();
            }
        }
        
        #endregion
    }
}
