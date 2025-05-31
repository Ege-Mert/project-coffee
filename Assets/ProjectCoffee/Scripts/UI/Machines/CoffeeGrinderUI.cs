using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;
using ProjectCoffee.Machines;
using ProjectCoffee.Core;

namespace ProjectCoffee.UI.Machines
{
    public class CoffeeGrinderUI : MachineUIBase<CoffeeGrinder>
    {
        [Header("Grinder UI")]
        [SerializeField] private Image beansLevelImage;
        [SerializeField] private Spinnable grindHandle;
        [SerializeField] private Button grindButton;
        [SerializeField] private GameObject groundCoffeeOutputVisual;
        
        [Header("Visual Settings")]
        [SerializeField] private bool keepHandleAlwaysVisible = true;
        [SerializeField] private float beansLevelAnimationDuration = 0.3f;
        
        [Header("Upgrade Indicators")]
        [SerializeField] private GameObject manualGrindIndicator;
        [SerializeField] private GameObject buttonGrindIndicator;
        [SerializeField] private GameObject autoGrindIndicator;
        
        private Coroutine grindingEffectCoroutine;
        
        protected override void SetupMachineSpecificUI()
        {
            SetupGrindHandle();
            SetupGrindButton();
            SubscribeToServiceEvents();
            
            UpdateBeansVisual(Machine.CurrentBeanCount);
            UpdateUIForUpgradeLevel(Machine.GetService()?.UpgradeLevel ?? 0);
        }
        
        private void SetupGrindHandle()
        {
            if (grindHandle != null)
            {
                grindHandle.OnSpinCompleted += OnHandleSpinCompleted;
                
                var service = Machine.GetService();
                
                // Fixed: More comprehensive interaction check
                grindHandle.CanInteractCustomCheck = () => {
                    if (service == null) 
                    {
                        Debug.Log("GrinderUI: Service is null");
                        return false;
                    }
                    
                    bool hasBeansCheck = Machine.CurrentBeanCount > 0;
                    bool upgradeCheck = service.UpgradeLevel == 0;
                    bool stateCheck = service.CurrentState == ProjectCoffee.Services.MachineState.Ready || 
                                     service.CurrentState == ProjectCoffee.Services.MachineState.Idle;
                    
                    Debug.Log($"GrinderUI Handle Check - Beans: {hasBeansCheck}, Upgrade: {upgradeCheck}, State: {stateCheck} (Current: {service.CurrentState})");
                    
                    return hasBeansCheck && upgradeCheck && stateCheck;
                };
                
                // Fixed: Handle is always visible until upgraded (level 0 only)
                bool shouldShowHandle = service?.UpgradeLevel == 0;
                grindHandle.gameObject.SetActive(shouldShowHandle);
                
                Debug.Log($"GrinderUI: Handle setup complete. Visible: {shouldShowHandle}, UpgradeLevel: {service?.UpgradeLevel}");
            }
            else
            {
                Debug.LogError("GrinderUI: grindHandle is null!");
            }
        }
        
        private void SetupGrindButton()
        {
            if (grindButton != null)
            {
                grindButton.onClick.RemoveAllListeners();
                grindButton.onClick.AddListener(OnGrindButtonClicked);
                
                var service = Machine.GetService();
                grindButton.gameObject.SetActive(service?.UpgradeLevel == 1);
                grindButton.interactable = Machine.CurrentBeanCount > 0;
            }
        }
        
        private void SubscribeToServiceEvents()
        {
            var machineService = Machine.GetService();
            if (machineService != null)
            {
                machineService.OnBeanCountChanged += UpdateBeansVisual;
                machineService.OnCoffeeOutputReady += HandleCoffeeOutputReady;
                machineService.OnCoffeeSizeUpgraded += HandleCoffeeSizeUpgraded;
                machineService.OnSpinCompleted += HandleSpinFeedback;
            }
        }
        
        protected override void HandleStateChanged(ProjectCoffee.Services.MachineState newState)
        {
            base.HandleStateChanged(newState);
            
            Debug.Log($"GrinderUI: State changed to {newState}");
            
            var service = Machine.GetService();
            if (grindButton != null && service != null && service.UpgradeLevel == 1)
            {
                grindButton.interactable = Machine.CurrentBeanCount > 0 && 
                                          newState == ProjectCoffee.Services.MachineState.Ready;
            }
            
            // Fixed: Update handle interaction when state changes
            if (grindHandle != null && service != null && service.UpgradeLevel == 0)
            {
                bool canInteract = Machine.CurrentBeanCount > 0 && 
                                  (newState == ProjectCoffee.Services.MachineState.Ready || 
                                   newState == ProjectCoffee.Services.MachineState.Idle);
                Debug.Log($"GrinderUI: Handle can interact: {canInteract} (State: {newState}, Beans: {Machine.CurrentBeanCount})");
            }
            
            if (newState == ProjectCoffee.Services.MachineState.Processing && grindingEffectCoroutine == null)
                grindingEffectCoroutine = StartCoroutine(GrindingVisualEffect());
            else if (newState != ProjectCoffee.Services.MachineState.Processing && grindingEffectCoroutine != null)
            {
                StopCoroutine(grindingEffectCoroutine);
                grindingEffectCoroutine = null;
            }
        }
        
        protected override void HandleUpgradeApplied(int level)
        {
            base.HandleUpgradeApplied(level);
            Debug.Log($"GrinderUI: Upgrade applied to level {level}");
            UpdateUIForUpgradeLevel(level);
        }
        
        private void UpdateUIForUpgradeLevel(int level)
        {
            Debug.Log($"GrinderUI: Updating UI for upgrade level {level}");
            
            if (grindHandle != null)
            {
                // Fixed: Handle is only visible for level 0, regardless of beans
                bool shouldShowHandle = level == 0;
                grindHandle.gameObject.SetActive(shouldShowHandle);
                
                if (shouldShowHandle)
                {
                    grindHandle.ResetSpinCount();
                    // Removed: grindHandle.ResetRotation(); - Don't reset position
                }
                
                Debug.Log($"GrinderUI: Handle visible: {shouldShowHandle} for level {level}");
            }
            
            if (grindButton != null)
            {
                grindButton.gameObject.SetActive(level == 1);
                grindButton.interactable = Machine.CurrentBeanCount > 0;
                Debug.Log($"GrinderUI: Button visible: {level == 1} for level {level}");
            }
            
            if (manualGrindIndicator != null)
                manualGrindIndicator.SetActive(level == 0);
                
            if (buttonGrindIndicator != null)
                buttonGrindIndicator.SetActive(level == 1);
                
            if (autoGrindIndicator != null)
                autoGrindIndicator.SetActive(level == 2);
        }
        
        private void OnHandleSpinCompleted(int spinCount)
        {
            Debug.Log($"GrinderUI: Handle spin completed! Count: {spinCount}");
            
            // Ensure the machine is in the right state before processing
            var service = Machine.GetService();
            if (service != null && service.CurrentState == ProjectCoffee.Services.MachineState.Idle && Machine.CurrentBeanCount > 0)
            {
                // Force the machine to ready state if it has beans but is idle
                service.ForceReadyState();
                Debug.Log("GrinderUI: Forced machine to ready state before processing spin");
            }
            
            // Call the machine's method to handle the spin completion
            Machine.OnHandleSpinCompleted();
            
            // Removed: No handle reset - let the handle stay where the user left it
        }
        
        private IEnumerator DelayedHandleReset()
        {
            yield return new WaitForSeconds(0.5f); // Small delay for feedback
            if (grindHandle != null)
            {
                grindHandle.ResetRotation();
            }
        }
        
        private void OnGrindButtonClicked()
        {
            Debug.Log("GrinderUI: Grind button clicked");
            // Call the machine's button press method
            Machine.OnButtonPress();
        }
        
        private void UpdateBeansVisual(int beanCount)
        {
            Debug.Log($"GrinderUI: Updating beans visual to {beanCount}");
            
            if (beansLevelImage != null)
            {
                float targetFillAmount = (float)beanCount / Machine.MaxBeanCount;
                beansLevelImage.DOFillAmount(targetFillAmount, beansLevelAnimationDuration)
                    .SetEase(Ease.InOutSine);
            }
            
            // Fixed: Handle always visible for level 0, regardless of bean count
            // No need to change visibility based on beans
            
            var machineService = Machine.GetService();
            if (grindButton != null && machineService?.UpgradeLevel == 1)
                grindButton.interactable = beanCount > 0;
        }
        
        private void HandleCoffeeOutputReady(GroundCoffee.GrindSize size)
        {
            Debug.Log($"GrinderUI: Coffee output ready with size {size}");
            
            if (groundCoffeeOutputVisual != null)
            {
                groundCoffeeOutputVisual.transform.DOScale(Vector3.one * 1.2f, 0.2f)
                    .SetLoops(2, LoopType.Yoyo)
                    .SetEase(Ease.OutQuad);
            }
            
            ShowNotification($"Ground coffee ready! Size: {size}");
        }
        
        private void HandleCoffeeSizeUpgraded(GroundCoffee.GrindSize newSize)
        {
            Debug.Log($"GrinderUI: Coffee size upgraded to {newSize}");
            
            if (groundCoffeeOutputVisual != null)
                groundCoffeeOutputVisual.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 2, 0.5f);
            
            ShowNotification($"Coffee upgraded to {newSize} size!");
        }
        
        private void HandleSpinFeedback(int spinCount)
        {
            Debug.Log($"GrinderUI: Spin feedback for count {spinCount}");
            
            // Visual feedback without moving the handle position
            if (grindHandle != null)
            {
                // Just play a small scale animation instead of rotation
                grindHandle.transform.DOScale(Vector3.one * 1.1f, 0.1f)
                    .SetLoops(2, LoopType.Yoyo);
            }
        }
        
        private IEnumerator GrindingVisualEffect()
        {
            Transform machineTransform = transform;
            Vector3 originalPosition = machineTransform.localPosition;
            
            var service = Machine.GetService();
            while (service != null && service.CurrentState == ProjectCoffee.Services.MachineState.Processing)
            {
                float shakeIntensity = 0.01f;
                machineTransform.localPosition = originalPosition + (Vector3)Random.insideUnitCircle * shakeIntensity;
                yield return new WaitForSeconds(0.05f);
            }
            
            machineTransform.localPosition = originalPosition;
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
                machineService.OnBeanCountChanged -= UpdateBeansVisual;
                machineService.OnCoffeeOutputReady -= HandleCoffeeOutputReady;
                machineService.OnCoffeeSizeUpgraded -= HandleCoffeeSizeUpgraded;
                machineService.OnSpinCompleted -= HandleSpinFeedback;
            }
            
            if (grindHandle != null)
                grindHandle.OnSpinCompleted -= OnHandleSpinCompleted;
            
            if (grindButton != null)
                grindButton.onClick.RemoveListener(OnGrindButtonClicked);
            
            if (grindingEffectCoroutine != null)
                StopCoroutine(grindingEffectCoroutine);
        }
    }
}