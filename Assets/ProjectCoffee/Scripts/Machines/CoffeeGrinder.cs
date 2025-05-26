using UnityEngine;
using UnityEngine.UI;
using ProjectCoffee.Services;
using ProjectCoffee.Machines;
using System.Collections;
using DG.Tweening;
using TMPro;


/// <summary>
/// Refactored coffee grinder that uses service pattern for logic
/// </summary>
public class CoffeeGrinder : Machine<CoffeeGrinderService, GrinderConfig>
{
    [Header("Grinder Specific UI")]
    [SerializeField] private Image beansLevelImage;
    [SerializeField] private Spinnable grindHandle;
    [SerializeField] private Button grindButton; // Changed from Clickable to standard Button
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
        
        Debug.Log("CoffeeGrinder: Start method called");
        
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
        
        // Configure button initial state
        if (grindButton != null)
        {
            // Start hidden, will be shown when upgraded
            grindButton.gameObject.SetActive(service?.UpgradeLevel >= 1);
            Debug.Log($"CoffeeGrinder: Initial button state - active: {grindButton.gameObject.activeSelf}, upgrade level: {service?.UpgradeLevel}");
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
            Debug.Log("CoffeeGrinder: Setting up grind button");
            
            // Remove any existing listeners to avoid duplicates
            grindButton.onClick.RemoveAllListeners();
            
            // Add click listener
            grindButton.onClick.AddListener(OnGrindButtonClicked);
            
            // Set interactability based on service state
            bool hasService = service != null;
            bool hasBeans = hasService && service.HasBeans;
            bool correctLevel = hasService && service.UpgradeLevel >= 1;
            bool canInteract = hasService && hasBeans && correctLevel;
            
            grindButton.interactable = canInteract;
            
            Debug.Log($"CoffeeGrinder: Button setup - hasService: {hasService}, hasBeans: {hasBeans}, correctLevel: {correctLevel}, canInteract: {canInteract}");
        }
        else
        {
            Debug.LogWarning("CoffeeGrinder: grindButton is null in SetupGrindButton!");
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
            grindButton.onClick.RemoveListener(OnGrindButtonClicked);
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
        Debug.Log("CoffeeGrinder: OnGrindButtonClicked called");
        
        if (service == null)
        {
            Debug.LogError("CoffeeGrinder: OnGrindButtonClicked - Service is null!");
            return;
        }
        
        // Verify we have beans to grind
        if (!service.HasBeans)
        {
            Debug.LogWarning("CoffeeGrinder: Cannot grind - no beans!");
            return;
        }
        
        // Disable button immediately to prevent multiple clicks
        if (grindButton != null)
        {
            grindButton.interactable = false;
        }
        
        // Start a direct coroutine for a more reliable approach
        StartCoroutine(DirectGrindingProcess());
    }
    
    /// <summary>
    /// Add manual method to grind in case the button fails
    /// </summary>
    public void GrindButtonClick()
    {
        Debug.Log("CoffeeGrinder: GrindButtonClick manual method called");
        OnGrindButtonClicked();
    }
    
    /// <summary>
    /// A more direct grinding process that bypasses some of the event chain
    /// </summary>
    private IEnumerator DirectGrindingProcess()
    {
        Debug.Log("CoffeeGrinder: Starting DirectGrindingProcess");
        
        // Disable button for the entire grinding process
        if (grindButton != null)
        {
            grindButton.interactable = false;
        }
        
        // Check if output zone is already at maximum capacity
        bool outputFull = currentGroundCoffee != null && 
                          currentGroundCoffee.GetGrindSize() == GroundCoffee.GrindSize.Large;
        
        if (outputFull)
        {
            Debug.Log("CoffeeGrinder: Cannot grind - output zone has maximum size coffee");
            UIManager.Instance.ShowNotification("Output zone full! Remove coffee first.");
            
            // Wait a moment before re-enabling button
            yield return new WaitForSeconds(0.5f);
            
            // Re-enable button
            if (grindButton != null)
            {
                grindButton.interactable = service != null && service.HasBeans;
            }
            
            yield break; // Exit the coroutine
        }
        
        // Process ALL beans continuously for level 1
        while (service != null && service.HasBeans)
        {
            // Check if coffee became max size during processing
            if (currentGroundCoffee != null && 
                currentGroundCoffee.GetGrindSize() == GroundCoffee.GrindSize.Large)
            {
                Debug.Log("CoffeeGrinder: Coffee reached maximum size, stopping processing");
                break;
            }
            
            // Tell the service we're starting to process
            service.OnButtonPressed();
            
            // Get grind time from config
            float grindTime = 3f; // Default fallback
            if (config is GrinderConfig grinderConfig)
            {
                grindTime = service.UpgradeLevel >= 2 ? 
                    grinderConfig.level2GrindTime : 
                    grinderConfig.level1GrindTime;
            }
            

            Debug.Log($"CoffeeGrinder: Grinding bean, remaining beans: {service.CurrentBeanFills}");
            
            // Wait for the grinding process
            float startTime = Time.time;
            
            // Update progress every frame
            while (Time.time < startTime + grindTime)
            {
                service.ProcessUpdate(Time.deltaTime);
                yield return null;
            }
            
            // If we still have beans, force state to Ready for next bean
            // BUT don't update button state here
            if (service.HasBeans)
            {
                service.ForceReadyState();
                
                // Brief pause between beans for visual feedback
                yield return new WaitForSeconds(0.5f);
            }
        }
        
        // ONLY re-enable button at the very end of processing
        if (grindButton != null)
        {
            grindButton.interactable = service != null && service.HasBeans;
        }
        
        Debug.Log("CoffeeGrinder: DirectGrindingProcess complete - all beans processed");
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
        
        // Update button interactability based on bean count
        if (grindButton != null && service != null && service.UpgradeLevel >= 1)
        {
            grindButton.interactable = beanCount > 0;
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
        
        // IMPORTANT: We now allow creation even if there's an existing coffee chunk
        // This is necessary for the direct process that bypasses events
        if (currentGroundCoffee != null)
        {
            Debug.Log("Already have ground coffee, upgrading it instead");
            UpgradeGroundCoffee(size);
            return;
        }
        
        Debug.Log("CoffeeGrinder: Starting ground coffee creation coroutine");
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
        
        Debug.Log($"CoffeeGrinder: HandleUpgradeApplied called with level {level}");
        
        // Update UI based on upgrade level
        if (grindHandle != null)
        {
            grindHandle.gameObject.SetActive(level == 0 && (keepHandleAlwaysVisible || service.HasBeans));
        }
        
        if (grindButton != null)
        {
            // Show button only for level 1, hide for levels 0 and 2
            grindButton.gameObject.SetActive(level == 1);
            // Make sure the button is interactable if there are beans
            grindButton.interactable = service != null && service.HasBeans;
            
            // Re-initialize button to ensure listeners are set up
            SetupGrindButton();
            
            Debug.Log($"CoffeeGrinder: Button active: {grindButton.gameObject.activeSelf}, interactable: {grindButton.interactable}");
        }
    }
    
    
    private void LateUpdate()
    {
        // Only process for level 2 (automatic grinder)
        if (service == null || service.UpgradeLevel != 2)
            return;
            
        // Check if we need to start processing
        EnsureAutomaticProcessing();
        
        // Continue processing if already in progress
        if (service.CurrentState == ProjectCoffee.Services.MachineState.Processing)
        {
            service.ProcessUpdate(Time.deltaTime);
        }
    }

    /// <summary>
    /// Ensures the automatic grinding is working properly for level 2
    /// </summary>
    private void EnsureAutomaticProcessing()
    {
        // Skip if no beans or already processing
        if (!service.HasBeans || service.CurrentState == ProjectCoffee.Services.MachineState.Processing)
            return;
            
        // Force state to Ready if needed (fixes issue when beans are already present)
        if (service.CurrentState != ProjectCoffee.Services.MachineState.Ready)
        {
            Debug.Log("CoffeeGrinder: Forcing state to Ready for automatic processing");
            service.ForceReadyState();
        }
        
        // Now check for auto-processing with proper state
        service.CheckAutoProcess();
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