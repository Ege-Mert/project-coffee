using UnityEngine;
using UnityEngine.UI;
using ProjectCoffee.Services;
using ProjectCoffee.Machines;
using System.Collections;
using DG.Tweening;

/// <summary>
/// Refactored coffee grinder that uses service pattern for logic
/// </summary>
public class CoffeeGrinder : Machine<CoffeeGrinderService, GrinderConfig>
{
    [Header("Grinder Specific UI")]
    [SerializeField] private Image beansLevelImage;
    [SerializeField] private Spinnable grindHandle;
    [SerializeField] private Clickable grindButton; // For level 1+ upgrade
    [SerializeField] private GroundCoffeeOutputZone groundCoffeeOutputZone;
    [SerializeField] private GameObject groundCoffeePrefab;
    
    [Header("Visual Settings")]
    [SerializeField] private bool keepHandleAlwaysVisible = true;
    
    private Coroutine processingCoroutine;
    private GroundCoffee currentGroundCoffee; // Keep track of the current coffee chunk
    
    protected override void InitializeService()
    {
        service = new CoffeeGrinderService(config as GrinderConfig);
    }
    
    protected override void Start()
    {
        base.Start();
        
        // Setup grinder-specific UI elements
        SetupGrindHandle();
        SetupGrindButton();
        SetupOutputZone();
        UpdateBeansVisual(0);
        
        // Subscribe to grinder-specific events
        if (service != null)
        {
            service.OnBeanCountChanged += UpdateBeansVisual;
            service.OnCoffeeOutputReady += CreateGroundCoffee;
            service.OnCoffeeSizeUpgraded += UpgradeGroundCoffee;
            service.OnSpinCompleted += HandleSpinFeedback;
        }
    }
    
    private void SetupGrindHandle()
    {
        if (grindHandle != null)
        {
            grindHandle.gameObject.SetActive(keepHandleAlwaysVisible || (service?.HasBeans ?? false));
            grindHandle.OnSpinCompleted += OnHandleSpinCompleted;
            
            // Set interaction check - only check if has beans for level 0
            grindHandle.CanInteractCustomCheck = () => {
                if (service == null) return false;
                return service.HasBeans && service.UpgradeLevel == 0;
            };
        }
    }
    
    private void SetupGrindButton()
    {
        if (grindButton != null)
        {
            // Start hidden, will be shown when upgraded
            grindButton.gameObject.SetActive(false);
            grindButton.OnClicked += OnGrindButtonClicked;
            
            // Set interaction check
            grindButton.CanInteractCustomCheck = () => service.HasBeans && service.UpgradeLevel >= 1;
        }
    }
    
    private void SetupOutputZone()
    {
        if (groundCoffeeOutputZone != null)
        {
            groundCoffeeOutputZone.SetParentGrinder(this);
        }
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        // Unsubscribe from grinder-specific events
        if (service != null)
        {
            service.OnBeanCountChanged -= UpdateBeansVisual;
            service.OnCoffeeOutputReady -= CreateGroundCoffee;
            service.OnCoffeeSizeUpgraded -= UpgradeGroundCoffee;
            service.OnSpinCompleted -= HandleSpinFeedback;
        }
        
        if (grindHandle != null)
        {
            grindHandle.OnSpinCompleted -= OnHandleSpinCompleted;
        }
        
        if (grindButton != null)
        {
            grindButton.OnClicked -= OnGrindButtonClicked;
        }
    }
    
    /// <summary>
    /// Add beans to the grinder
    /// </summary>
    public void AddBeans(int amount)
    {
        service?.AddBeans(amount);
    }
    
    /// <summary>
    /// Handle spin completion from UI
    /// </summary>
    private void OnHandleSpinCompleted(int spinCount)
    {
        // For progressive upgrading, we want to process each spin individually
        service?.OnHandleSpinCompleted();
    }
    
    /// <summary>
    /// Handle grind button click
    /// </summary>
    private void OnGrindButtonClicked()
    {
        service?.OnButtonPressed();
        
        // Start processing coroutine for level 1+
        if (service.UpgradeLevel >= 1 && service.CurrentState == MachineState.Processing)
        {
            if (processingCoroutine != null)
            {
                StopCoroutine(processingCoroutine);
            }
            processingCoroutine = StartCoroutine(ProcessGrinding());
        }
    }
    
    /// <summary>
    /// Processing coroutine for timed grinding (Level 1+)
    /// </summary>
    private IEnumerator ProcessGrinding()
    {
        float elapsedTime = 0f;
        
        // Start effects
        if (processingParticles != null)
        {
            processingParticles.Play();
        }
        
        if (processStartSound != null)
        {
            processStartSound.Play();
        }
        
        // Update loop
        while (service.CurrentState == MachineState.Processing)
        {
            elapsedTime += Time.deltaTime;
            service.ProcessUpdate(Time.deltaTime);
            yield return null;
        }
        
        // Stop effects
        if (processingParticles != null)
        {
            processingParticles.Stop();
        }
        
        processingCoroutine = null;
    }
    
    /// <summary>
    /// Update beans visual based on count
    /// </summary>
    private void UpdateBeansVisual(int beanCount)
    {
        if (beansLevelImage != null)
        {
            float fillAmount = (float)beanCount / service.MaxBeanFills;
            beansLevelImage.fillAmount = fillAmount;
        }
        
        // Update handle visibility
        if (grindHandle != null && !keepHandleAlwaysVisible)
        {
            grindHandle.gameObject.SetActive(beanCount > 0);
        }
    }
    
    /// <summary>
    /// Create ground coffee output
    /// </summary>
    private void CreateGroundCoffee(GroundCoffee.GrindSize size)
    {
        if (groundCoffeePrefab == null || groundCoffeeOutputZone == null)
        {
            Debug.LogError("Cannot create ground coffee: Missing prefab or output zone!");
            return;
        }
        
        // Don't create a new one if we already have one
        if (currentGroundCoffee != null)
        {
            Debug.Log("Already have ground coffee, will upgrade instead");
            return;
        }
        
        StartCoroutine(CreateGroundCoffeeCoroutine(size));
    }
    
    private IEnumerator CreateGroundCoffeeCoroutine(GroundCoffee.GrindSize size)
    {
        // Wait for end of frame to ensure UI is ready
        yield return new WaitForEndOfFrame();
        
        // Instantiate ground coffee
        GameObject coffeeObj = Instantiate(groundCoffeePrefab, groundCoffeeOutputZone.transform);
        currentGroundCoffee = coffeeObj.GetComponent<GroundCoffee>();
        
        if (currentGroundCoffee != null)
        {
            // Set properties
            currentGroundCoffee.SetGrindSize(size);
            
            // Set amount based on config
            float amount = GetAmountForSize(size);
            currentGroundCoffee.SetAmount(amount);
            
            // Position correctly
            RectTransform rectTransform = coffeeObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.zero;
                
                // Appearance animation
                rectTransform.localScale = Vector3.zero;
                rectTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            }
            
            UIManager.Instance.ShowNotification($"Created {size} ground coffee!");
        }
    }
    
    /// <summary>
    /// Upgrade the existing ground coffee chunk
    /// </summary>
    private void UpgradeGroundCoffee(GroundCoffee.GrindSize newSize)
    {
        if (currentGroundCoffee == null)
        {
            Debug.LogError("No ground coffee to upgrade!");
            return;
        }
        
        // Update the size
        currentGroundCoffee.SetGrindSize(newSize);
        
        // Update the amount
        float newAmount = GetAmountForSize(newSize);
        currentGroundCoffee.SetAmount(newAmount);
        
        // Visual feedback
        RectTransform coffeeRect = currentGroundCoffee.GetComponent<RectTransform>();
        if (coffeeRect != null)
        {
            coffeeRect.DOPunchScale(Vector3.one * 0.2f, 0.3f, 2, 0.5f);
        }
        
        UIManager.Instance.ShowNotification($"Ground coffee upgraded to {newSize} size ({newAmount}g)");
    }
    
    private float GetAmountForSize(GroundCoffee.GrindSize size)
    {
        if (config != null && config is GrinderConfig grinderConfig)
        {
            switch (size)
            {
                case GroundCoffee.GrindSize.Small:
                    return grinderConfig.groundCoffeeSizes.Length > 0 ? grinderConfig.groundCoffeeSizes[0] : 6f;
                case GroundCoffee.GrindSize.Medium:
                    return grinderConfig.groundCoffeeSizes.Length > 1 ? grinderConfig.groundCoffeeSizes[1] : 12f;
                case GroundCoffee.GrindSize.Large:
                    return grinderConfig.groundCoffeeSizes.Length > 2 ? grinderConfig.groundCoffeeSizes[2] : 18f;
                default:
                    return 6f;
            }
        }
        return 6f;
    }
    
    /// <summary>
    /// Handle spin feedback (visual/audio)
    /// </summary>
    private void HandleSpinFeedback(int spinCount)
    {
        // You can add additional feedback here if needed
        Debug.Log($"Service spin count: {spinCount}");
    }
    
    protected override void HandleUpgradeApplied(int level)
    {
        base.HandleUpgradeApplied(level);
        
        // Update UI based on upgrade level
        if (grindHandle != null)
        {
            grindHandle.gameObject.SetActive(level == 0 && (keepHandleAlwaysVisible || service.HasBeans));
        }
        
        if (grindButton != null)
        {
            grindButton.gameObject.SetActive(level >= 1);
        }
    }
    
    private void Update()
    {
        // Check for auto-processing at level 2
        if (service != null && service.UpgradeLevel >= 2)
        {
            service.CheckAutoProcess();
        }
    }
    
    // Public methods for external access
    public void OnGroundCoffeeRemoved()
    {
        currentGroundCoffee = null;
        service?.OnGroundCoffeeRemoved();
    }
    
    public bool CanAddBeans => service?.CanAddBeans ?? false;
    public int CurrentBeanCount => service?.CurrentBeanFills ?? 0;
    public int MaxBeanCount => service?.MaxBeanFills ?? 3;
}
