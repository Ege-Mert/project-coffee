using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;
using ProjectCoffee.Machines.Grinder;
using ProjectCoffee.Services.Grinder;
using ProjectCoffee.Services;
using ProjectCoffee.Core;

namespace ProjectCoffee.UI.Machines.Grinder
{
    /// <summary>
    /// Simplified grinder UI that focuses on visual representation and user interaction
    /// </summary>
    public class GrinderUI : MachineUIBase<GrinderMachine>
    {
        [Header("Grinder UI Components")]
        [SerializeField] private Image beansLevelImage;
        [SerializeField] private Spinnable grindHandle;
        [SerializeField] private Button grindButton;
        [SerializeField] private Button stopButton;
        [SerializeField] private GameObject groundCoffeeOutputVisual;
        
        [Header("Visual Settings")]
        [SerializeField] private float beansLevelAnimationDuration = 0.3f;
        
        [Header("Upgrade Indicators")]
        [SerializeField] private GameObject manualGrindIndicator;
        [SerializeField] private GameObject buttonGrindIndicator;
        [SerializeField] private GameObject autoGrindIndicator;
        
        private GrinderService service;
        private Coroutine grindingEffectCoroutine;
        
        #region Initialization
        
        protected override void SetupMachineSpecificUI()
        {
            GetServiceReference();
            
            if (service != null)
            {
                SetupInteractionElements();
                SubscribeToServiceEvents();
                
                // Initialize visual state
                UpdateBeansVisual(Machine.CurrentBeanCount);
                UpdateUIForUpgradeLevel(service.UpgradeLevel);
            }
            else
            {
                Debug.LogError("GrinderUI: Failed to get service even after delayed setup!");
            }
        }
        
        private void GetServiceReference()
        {
            service = Machine?.GetService();
            if (service == null)
            {
                Debug.LogWarning("GrinderUI: Service not yet available, will retry...");
            }
            else
            {
                Debug.Log("GrinderUI: Successfully got service reference");
            }
        }
        
        private void SetupInteractionElements()
        {
            SetupGrindHandle();
            SetupGrindButton();
        }
        
        #endregion
        
        #region Handle Setup
        
        private void SetupGrindHandle()
        {
            if (grindHandle == null)
            {
                Debug.LogError("GrinderUI: grindHandle is null!");
                return;
            }
            
            grindHandle.OnSpinCompleted += OnHandleSpinCompleted;
            
            // Set up interaction check
            grindHandle.CanInteractCustomCheck = () => CanUseHandle();
            
            // Initial visibility
            UpdateHandleVisibility();
            
            Debug.Log("GrinderUI: Handle setup complete");
        }
        
        private bool CanUseHandle()
        {
            if (service == null) return false;
            
            bool hasBeansCheck = Machine.CurrentBeanCount > 0;
            bool upgradeCheck = service.UpgradeLevel == 0;
            bool stateCheck = service.CurrentState == MachineState.Ready || 
                             service.CurrentState == MachineState.Idle;
            
            return hasBeansCheck && upgradeCheck && stateCheck;
        }
        
        private void UpdateHandleVisibility()
        {
            if (grindHandle != null && service != null)
            {
                bool shouldShow = service.UpgradeLevel == 0;
                grindHandle.gameObject.SetActive(shouldShow);
                
                if (shouldShow)
                {
                    grindHandle.ResetSpinCount();
                }
            }
        }
        
        #endregion
        
        #region Button Setup
        
        private void SetupGrindButton()
        {
            if (grindButton == null) return;
            
            grindButton.onClick.RemoveAllListeners();
            grindButton.onClick.AddListener(OnGrindButtonClicked);
            
            if (stopButton != null)
            {
                stopButton.onClick.RemoveAllListeners();
                stopButton.onClick.AddListener(OnStopButtonClicked);
            }
            
            UpdateButtonVisibility();
        }
        
        private void UpdateButtonVisibility()
        {
            if (grindButton != null && service != null)
            {
                bool shouldShow = service.UpgradeLevel == 1;
                grindButton.gameObject.SetActive(shouldShow);
                grindButton.interactable = shouldShow && Machine.CurrentBeanCount > 0;
                
                if (stopButton != null)
                {
                    stopButton.gameObject.SetActive(shouldShow);
                    stopButton.interactable = shouldShow;
                }
            }
        }
        
        #endregion
        
        #region Event Subscriptions
        
        private void SubscribeToServiceEvents()
        {
            if (service != null)
            {
                service.OnBeanCountChanged += UpdateBeansVisual;
                service.OnCoffeeOutputReady += HandleCoffeeOutputReady;
                service.OnCoffeeSizeUpgraded += HandleCoffeeSizeUpgraded;
                service.OnSpinCompleted += HandleSpinFeedback;
                service.OnUpgradeApplied += HandleUpgradeApplied;
            }
        }
        
        private void UnsubscribeFromServiceEvents()
        {
            if (service != null)
            {
                service.OnBeanCountChanged -= UpdateBeansVisual;
                service.OnCoffeeOutputReady -= HandleCoffeeOutputReady;
                service.OnCoffeeSizeUpgraded -= HandleCoffeeSizeUpgraded;
                service.OnSpinCompleted -= HandleSpinFeedback;
                service.OnUpgradeApplied -= HandleUpgradeApplied;
            }
        }
        
        #endregion
        
        #region User Interaction Handlers
        
        private void OnHandleSpinCompleted(int spinCount)
        {
            Debug.Log($"GrinderUI: Handle spin completed! Count: {spinCount}");
            
            if (Machine == null || service == null) return;
            
            // Ensure machine is ready before processing
            if (service.CurrentState == MachineState.Idle && Machine.CurrentBeanCount > 0)
            {
                service.ForceReadyState();
            }
            
            Machine.OnHandleSpinCompleted();
        }
        
        private void OnGrindButtonClicked()
        {
            Debug.Log("GrinderUI: Grind button clicked");
            if (Machine != null)
            {
                Machine.OnButtonPress();
            }
        }
        
        private void OnStopButtonClicked()
        {
            Debug.Log("GrinderUI: Stop button clicked");
            if (Machine != null)
            {
                Machine.StopContinuousProcessing();
            }
        }
        
        #endregion
        
        #region Visual Updates
        
        private void UpdateBeansVisual(int beanCount)
        {
            Debug.Log($"GrinderUI: Updating beans visual to {beanCount}");
            
            if (beansLevelImage != null && Machine != null)
            {
                float targetFillAmount = (float)beanCount / Machine.MaxBeanCount;
                beansLevelImage.DOFillAmount(targetFillAmount, beansLevelAnimationDuration)
                    .SetEase(Ease.InOutSine);
            }
            
            UpdateInteractionStates();
        }
        
        private void UpdateInteractionStates()
        {
            // Update button interactability
            if (grindButton != null && service?.UpgradeLevel == 1 && Machine != null)
            {
                grindButton.interactable = Machine.CurrentBeanCount > 0;
            }
            
            // Handle interaction updates are handled through the CanInteractCustomCheck
        }
        
        #endregion
        
        #region State Change Handling
        
        protected override void HandleStateChanged(MachineState newState)
        {
            base.HandleStateChanged(newState);
            
            Debug.Log($"GrinderUI: State changed to {newState}");
            
            UpdateInteractionStates();
            HandleProcessingVisuals(newState);
        }
        
        private void HandleProcessingVisuals(MachineState state)
        {
            if (state == MachineState.Processing && grindingEffectCoroutine == null)
            {
                grindingEffectCoroutine = StartCoroutine(GrindingVisualEffect());
            }
            else if (state != MachineState.Processing && grindingEffectCoroutine != null)
            {
                StopCoroutine(grindingEffectCoroutine);
                grindingEffectCoroutine = null;
            }
        }
        
        #endregion
        
        #region Upgrade Handling
        
        protected override void HandleUpgradeApplied(int level)
        {
            base.HandleUpgradeApplied(level);
            Debug.Log($"GrinderUI: Upgrade applied to level {level}");
            UpdateUIForUpgradeLevel(level);
        }
        
        private void UpdateUIForUpgradeLevel(int level)
        {
            Debug.Log($"GrinderUI: Updating UI for upgrade level {level}");
            
            UpdateHandleForUpgrade(level);
            UpdateButtonForUpgrade(level);
            UpdateUpgradeIndicators(level);
        }
        
        private void UpdateHandleForUpgrade(int level)
        {
            if (grindHandle != null)
            {
                bool shouldShow = level == 0;
                grindHandle.gameObject.SetActive(shouldShow);
                
                if (shouldShow)
                {
                    grindHandle.ResetSpinCount();
                }
            }
        }
        
        private void UpdateButtonForUpgrade(int level)
        {
            if (grindButton != null)
            {
                bool shouldShow = level == 1;
                grindButton.gameObject.SetActive(shouldShow);
                grindButton.interactable = shouldShow && Machine.CurrentBeanCount > 0;
                
                if (stopButton != null)
                {
                    stopButton.gameObject.SetActive(shouldShow);
                    stopButton.interactable = shouldShow;
                }
            }
        }
        
        private void UpdateUpgradeIndicators(int level)
        {
            if (manualGrindIndicator != null)
                manualGrindIndicator.SetActive(level == 0);
                
            if (buttonGrindIndicator != null)
                buttonGrindIndicator.SetActive(level == 1);
                
            if (autoGrindIndicator != null)
                autoGrindIndicator.SetActive(level == 2);
        }
        
        #endregion
        
        #region Coffee Output Handling
        
        private void HandleCoffeeOutputReady(GroundCoffee.GrindSize size)
        {
            Debug.Log($"GrinderUI: Coffee output ready with size {size}");
            
            PlayCoffeeReadyEffect();
            ShowNotification($"Ground coffee ready! Size: {size}");
        }
        
        private void HandleCoffeeSizeUpgraded(GroundCoffee.GrindSize newSize)
        {
            Debug.Log($"GrinderUI: Coffee size upgraded to {newSize}");
            
            PlayCoffeeUpgradeEffect();
            ShowNotification($"Coffee upgraded to {newSize} size!");
        }
        
        private void HandleSpinFeedback(int spinCount)
        {
            Debug.Log($"GrinderUI: Spin feedback for count {spinCount}");
            
            // Visual feedback without moving the handle position
            if (grindHandle != null)
            {
                grindHandle.transform.DOScale(Vector3.one * 1.1f, 0.1f)
                    .SetLoops(2, LoopType.Yoyo);
            }
        }
        
        #endregion
        
        #region Visual Effects
        
        private void PlayCoffeeReadyEffect()
        {
            if (groundCoffeeOutputVisual != null)
            {
                groundCoffeeOutputVisual.transform.DOScale(Vector3.one * 1.2f, 0.2f)
                    .SetLoops(2, LoopType.Yoyo)
                    .SetEase(Ease.OutQuad);
            }
        }
        
        private void PlayCoffeeUpgradeEffect()
        {
            if (groundCoffeeOutputVisual != null)
            {
                groundCoffeeOutputVisual.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 2, 0.5f);
            }
        }
        
        private IEnumerator GrindingVisualEffect()
        {
            Transform machineTransform = transform;
            Vector3 originalPosition = machineTransform.localPosition;
            
            while (service != null && service.CurrentState == MachineState.Processing)
            {
                float shakeIntensity = 0.01f;
                machineTransform.localPosition = originalPosition + (Vector3)Random.insideUnitCircle * shakeIntensity;
                yield return new WaitForSeconds(0.05f);
            }
            
            machineTransform.localPosition = originalPosition;
        }
        
        #endregion
        
        #region Utility
        
        private void ShowNotification(string message)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowNotification(message);
        }
        
        #endregion
        
        #region Cleanup
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            UnsubscribeFromServiceEvents();
            
            if (grindHandle != null)
                grindHandle.OnSpinCompleted -= OnHandleSpinCompleted;
            
            if (grindButton != null)
                grindButton.onClick.RemoveListener(OnGrindButtonClicked);
                
            if (stopButton != null)
                stopButton.onClick.RemoveListener(OnStopButtonClicked);
            
            if (grindingEffectCoroutine != null)
                StopCoroutine(grindingEffectCoroutine);
        }
        
        #endregion
    }
}
