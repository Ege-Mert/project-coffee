using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectCoffee.Services;
using ProjectCoffee.Services.Interfaces;
using ProjectCoffee.Machines;
using ProjectCoffee.Core.Services;
using DG.Tweening;

/// <summary>
/// Refactored coffee gramming machine that uses service pattern for logic
/// </summary>
public class CoffeeGrammingMachine : Machine<CoffeeGrammingService, GrammingMachineConfig>
{
    [Header("Gramming Machine Specific UI")]
    [SerializeField] private DropZone portafilterZone;
    [SerializeField] private DropZone groundCoffeeZone;
    [SerializeField] private Holdable grammingButton;
    [SerializeField] private Button autoDoseButton; // New button for Level 1
    [SerializeField] private TMP_Text portafilterGramText;
    [SerializeField] private TMP_Text storageGramText;
    [SerializeField] private Image qualityIndicator;
    [SerializeField] private Gradient qualityGradient;
    [SerializeField] private GameObject autoDosingIndicator; // Visual indicator for Level 2
    
    private Portafilter currentPortafilter;
    
    // Add this field near your other private fields
    private float autoOperationCheckTimer = 0f;
    
    protected override void InitializeService()
    {
        service = new CoffeeGrammingService(config as GrammingMachineConfig);
    }
    
    protected override void Start()
    {
        base.Start();
        
        // Setup UI elements
        ConfigureDropZones();
        ConfigureGrammingButton();
        ConfigureAutoDoseButton();
        
        // Subscribe to gramming-specific events
        if (service != null)
        {
            service.OnCoffeeAmountChanged += UpdateStorageDisplay;
            service.OnPortafilterFillChanged += UpdatePortafilterDisplay;
            service.OnQualityEvaluated += UpdateQualityIndicator;
            service.OnAutoDoseCompleted += OnAutoDoseCompleted;
            service.OnAutoDoseStarted += OnAutoDoseStarted;
        }
        
        // Configure UI based on initial upgrade level
        UpdateUIForUpgradeLevel(service?.UpgradeLevel ?? 0);
        UpdateUI();
    }
    
    private void ConfigureDropZones()
    {
        if (portafilterZone != null)
        {
            Debug.Log("Configuring portafilter drop zone");
            portafilterZone.AcceptPredicate = (item) => {
                bool isPortafilter = item is Portafilter;
                bool canAccept = isPortafilter && !service.HasPortafilter;
                Debug.Log($"Portafilter zone accept check: isPortafilter={isPortafilter}, canAccept={canAccept}");
                return canAccept;
            };
        }
        else
        {
            Debug.LogError("Portafilter zone is null!");
        }
        
        if (groundCoffeeZone != null)
        {
            Debug.Log("Configuring ground coffee drop zone");
            groundCoffeeZone.AcceptPredicate = (item) => {
                bool isGroundCoffee = item is GroundCoffee;
                Debug.Log($"Ground coffee zone accept check: isGroundCoffee={isGroundCoffee}");
                return isGroundCoffee;
            };
        }
        else
        {
            Debug.LogError("Ground coffee zone is null!");
        }
    }
    
    private void ConfigureGrammingButton()
    {
        if (grammingButton != null)
        {
            // Set the CanInteract delegate directly on Holdable
            grammingButton.CanInteract = () => {
                if (service == null) return false;
                bool hasPortafilter = service.HasPortafilter;
                bool hasCoffee = service.StoredCoffeeAmount > 0;
                bool correctLevel = service.UpgradeLevel == 0; // Only active at level 0
                bool result = hasPortafilter && hasCoffee && correctLevel;
                Debug.Log($"GrammingButton CanInteract check: HasPortafilter={hasPortafilter}, HasCoffee={hasCoffee}, Storage={service.StoredCoffeeAmount}g, Level={service.UpgradeLevel}, Result={result}");
                return result;
            };
            
            // Also set isActive to true in case it was disabled
            var buttonComponent = grammingButton.GetComponent<Button>();
            if (buttonComponent != null)
            {
                buttonComponent.interactable = true;
            }
            
            grammingButton.OnHold = OnGrammingButtonHold;
            grammingButton.OnHoldRelease = OnGrammingButtonRelease;
        }
    }
    
    private void ConfigureAutoDoseButton()
    {
        if (autoDoseButton != null)
        {
            // Remove any existing listeners
            autoDoseButton.onClick.RemoveAllListeners();
            
            // Add click listener for the auto dose button
            autoDoseButton.onClick.AddListener(OnAutoDoseButtonClicked);
            
            // Initially disabled if not at level 1
            autoDoseButton.gameObject.SetActive(service?.UpgradeLevel == 1);
            autoDoseButton.interactable = false; // Will be updated in Update
        }
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        // Unsubscribe from events
        if (service != null)
        {
            service.OnCoffeeAmountChanged -= UpdateStorageDisplay;
            service.OnPortafilterFillChanged -= UpdatePortafilterDisplay;
            service.OnQualityEvaluated -= UpdateQualityIndicator;
            service.OnAutoDoseCompleted -= OnAutoDoseCompleted;
            service.OnAutoDoseStarted -= OnAutoDoseStarted;
        }
        
        // Clean up auto dose button listener
        if (autoDoseButton != null)
        {
            autoDoseButton.onClick.RemoveListener(OnAutoDoseButtonClicked);
        }
    }
    
    private void Update()
    {
        CheckPortafilterPresence();
        
        // Update the auto dose button interactability
        UpdateAutoDoseButtonState();
        
        // Use service locator to get service when needed
        var grammingService = ServiceLocator.Instance.GetService<IGrammingService>();
        
        // For Level 2: If portafilter amount changes, update the visual portafilter
        if (grammingService?.UpgradeLevel == 2 && currentPortafilter != null)
        {
            float serviceAmount = grammingService.PortafilterCoffeeAmount;
            float currentAmount = currentPortafilter.GetItemAmount("ground_coffee");
            
            // If there's a mismatch between service and visual state, update the visual
            if (serviceAmount > 0 && Mathf.Abs(serviceAmount - currentAmount) > 0.01f)
            {
                Debug.Log($"Syncing portafilter visual: Service amount={serviceAmount}g, Visual amount={currentAmount}g");
                currentPortafilter.Clear();
                currentPortafilter.TryAddItem("ground_coffee", serviceAmount);
            }
        }
        
        // Update visual feedback for processing state
        if (service != null && service.CurrentState == MachineState.Processing)
        {
            // Show processing particles when in processing state
            if (processingParticles != null && !processingParticles.isPlaying)
            {
                Debug.Log("Starting processing particles");
                processingParticles.Play();
            }
        }
        else
        {
            // Stop particles when not processing
            if (processingParticles != null && processingParticles.isPlaying)
            {
                Debug.Log("Stopping processing particles");
                processingParticles.Stop();
            }
        }
    }
    
    /// <summary>
    /// LateUpdate runs after all Updates 
    /// </summary>
    private void LateUpdate()
    {
        // Use this for any additional checks that should happen after regular updates
    }
    
    private void UpdateAutoDoseButtonState()
    {
        if (autoDoseButton != null && service?.UpgradeLevel == 1)
        {
            bool hasPortafilter = service.HasPortafilter;
            bool hasCoffee = service.StoredCoffeeAmount > 0;
            bool emptyPortafilter = service.PortafilterCoffeeAmount <= 0;
            
            // Only interactable if there's a portafilter, it's empty, and there's coffee in storage
            autoDoseButton.interactable = hasPortafilter && hasCoffee && emptyPortafilter;
        }
    }
    
    private void CheckPortafilterPresence()
    {
        if (portafilterZone == null) return;
        
        // Detect when portafilter is added
        if (portafilterZone.transform.childCount > 0 && currentPortafilter == null)
        {
            Portafilter portafilter = portafilterZone.transform.GetChild(0).GetComponent<Portafilter>();
            if (portafilter != null)
            {
                currentPortafilter = portafilter;
                service.SetPortafilterPresent(true);
                
                // Update UI
                UpdatePortafilterDisplay(service.PortafilterCoffeeAmount);
                
                // Handle auto-dose for level 1
                if (service.UpgradeLevel == 1 && service.StoredCoffeeAmount > 0)
                {
                    // Not implemented in this version - would trigger auto 18g dose
                }
            }
        }
        // Detect when portafilter is removed
        else if (portafilterZone.transform.childCount == 0 && currentPortafilter != null)
        {
            currentPortafilter = null;
            service.SetPortafilterPresent(false);
            UpdatePortafilterDisplay(0f);
        }
    }
    
    private void OnGrammingButtonHold(float duration)
    {
        Debug.Log($"OnGrammingButtonHold: duration={duration}, hasPortafilter={currentPortafilter != null}");
        
        // Update the service with the hold duration
        if (service != null && service.HasPortafilter && service.StoredCoffeeAmount > 0)
        {
            float previousAmount = service.PortafilterCoffeeAmount;
            
            // Process the hold with deltaTime
            service.OnDispensingHold(Time.deltaTime);
            
            float newAmount = service.PortafilterCoffeeAmount;
            
            // Only update the visual portafilter if the amount changed
            if (currentPortafilter != null && newAmount != previousAmount)
            {
                Debug.Log($"Updating portafilter coffee from {previousAmount}g to {newAmount}g");
                
                // Clear and add the new amount
                currentPortafilter.Clear();
                bool added = currentPortafilter.TryAddItem("ground_coffee", newAmount);
                Debug.Log($"TryAddItem result: {added}, Portafilter now has: {currentPortafilter.GetItemAmount("ground_coffee")}g");
            }
        }
        
        // Visual feedback
        if (processingParticles != null && !processingParticles.isPlaying && service.CurrentState == MachineState.Processing)
        {
            processingParticles.Play();
        }
    }
    
    /// <summary>
    /// Handle auto-dose button click for level 1
    /// </summary>
    private void OnAutoDoseButtonClicked()
    {
        Debug.Log("Auto-dose button clicked");
        
        if (service == null || !service.HasPortafilter || service.StoredCoffeeAmount <= 0)
        {
            Debug.LogWarning("Cannot auto-dose: Invalid conditions");
            return;
        }
        
        // Disable button immediately to prevent multiple clicks
        if (autoDoseButton != null)
        {
            autoDoseButton.interactable = false;
        }
        
        // Visual feedback
        if (processingParticles != null)
        {
            processingParticles.Play();
        }
        
        if (processStartSound != null)
        {
            processStartSound.Play();
        }
        
        // Start the auto-dosing process
        StartCoroutine(AutoDoseProcess());
    }
    
    /// <summary>
    /// Process for auto-dosing at level 1
    /// </summary>
    private System.Collections.IEnumerator AutoDoseProcess()
    {
        // Start processing
        service.StartProcessing();
        
        // Wait for the auto-dosing time from config
        float processingTime = 2f; // Default fallback
        
        // Get the proper processing time from config
        if (config is GrammingMachineConfig grammingConfig)
        {
            processingTime = grammingConfig.level1AutoDoseTime;
            Debug.Log($"Using configured auto-dose time: {processingTime}s");
        }
        else
        {
            Debug.LogWarning("AutoDoseProcess: Could not access GrammingMachineConfig, using default time");
        }
        
        float startTime = Time.time;
        
        while (Time.time < startTime + processingTime)
        {
            // Update progress
            float progress = (Time.time - startTime) / processingTime;
            service.UpdateProgress(progress);
            yield return null;
        }
        
        // Complete the auto-dosing
        service.PerformAutoDose();
        
        // Update the visual portafilter
        if (currentPortafilter != null)
        {
            float newAmount = service.PortafilterCoffeeAmount;
            currentPortafilter.Clear();
            currentPortafilter.TryAddItem("ground_coffee", newAmount);
        }
        
        // Stop visual feedback
        if (processingParticles != null)
        {
            processingParticles.Stop();
        }
        
        if (processCompleteSound != null)
        {
            processCompleteSound.Play();
        }
        
        // Re-enable button after a configurable delay
        float cooldownDelay = 0.5f; // Default value
        yield return new WaitForSeconds(cooldownDelay);
        UpdateAutoDoseButtonState();
    }
    
    /// <summary>
    /// Handler for auto-dose completion event
    /// </summary>
    private void OnAutoDoseCompleted()
    {
        Debug.Log("Auto-dose completed, updating portafilter visual");
        
        if (currentPortafilter != null && service != null)
        {
            float newAmount = service.PortafilterCoffeeAmount;
            Debug.Log($"Updating portafilter with {newAmount}g of coffee");
            
            // Clear and add the correct amount to the portafilter
            currentPortafilter.Clear();
            currentPortafilter.TryAddItem("ground_coffee", newAmount);
            
            // Provide visual feedback
            if (processingParticles != null)
            {
                processingParticles.Stop();
            }
            
            if (processCompleteSound != null)
            {
                processCompleteSound.Play();
            }
        }
    }
    
    /// <summary>
    /// Handler for auto-dose started event
    /// </summary>
    private void OnAutoDoseStarted()
    {
        Debug.Log("Auto-dose started, showing processing effects");
        
        // Start visual feedback
        if (processingParticles != null && !processingParticles.isPlaying)
        {
            processingParticles.Play();
        }
        
        if (processStartSound != null)
        {
            processStartSound.Play();
        }
    }
    
    private void OnGrammingButtonRelease(float heldDuration)
    {
        service?.OnDispensingRelease();
        
        // Stop visual feedback
        if (processingParticles != null)
        {
            processingParticles.Stop();
        }
    }
    
    /// <summary>
    /// Called when ground coffee is dropped into the input zone
    /// </summary>
    public void OnGroundCoffeeDropped(Draggable item)
    {
        if (item is GroundCoffee groundCoffee)
        {
            float coffeeAmount = groundCoffee.GetAmount();
            Debug.Log($"OnGroundCoffeeDropped: Adding {coffeeAmount}g to storage");
            bool added = service.AddCoffee(coffeeAmount);
            Debug.Log($"Coffee added: {added}, New storage amount: {service.StoredCoffeeAmount}g");
            
            // Visual feedback
            if (processingParticles != null)
            {
                processingParticles.Play();
                Invoke("StopParticles", 1.0f);
            }
            
            // Sound effect
            if (processStartSound != null)
            {
                processStartSound.Play();
            }
            
            // Consume the ground coffee
            DOTween.Kill(groundCoffee.transform);
            Destroy(groundCoffee.gameObject, 0.5f);
            
            // DIRECT AUTO-DOSING CHECK FOR LEVEL 2
            // If we're at level 2 and have a portafilter, directly trigger auto-dosing
            if (service.UpgradeLevel == 2 && currentPortafilter != null)
            {
                Debug.Log("OnGroundCoffeeDropped: Level 2 with portafilter - DIRECTLY triggering auto-dosing");
                
                // Use a small delay to allow UI to update
                Invoke("DirectAutoDosingCheck", 0.5f);
            }
        }
    }
    
    /// <summary>
    /// Directly checks and triggers auto-dosing, bypassing the request system
    /// </summary>
    private void DirectAutoDosingCheck()
    {
        Debug.Log("DirectAutoDosingCheck: Forcing auto-dosing check");
        
        // Ensure the machine is in the right state
        if (service.CurrentState != MachineState.Ready)
        {
            Debug.Log($"DirectAutoDosingCheck: Forcing state from {service.CurrentState} to Ready");
            // Force the machine to be in a ready state
            if (service.CurrentState == MachineState.Idle)
            {
                service.TransitionToState(MachineState.Ready);
            }
        }
        
        // Check for auto-dosing directly - this should bypass any state checks
        StartCoroutine(ForceAutoDosing());
    }
    
    /// <summary>
    /// Force auto-dosing with a small delay
    /// </summary>
    private System.Collections.IEnumerator ForceAutoDosing()
    {
        // Small delay to ensure all state transitions are complete
        yield return new WaitForSeconds(0.2f);
        
        if (service != null && service.UpgradeLevel == 2 && 
            currentPortafilter != null && service.HasPortafilter && 
            service.StoredCoffeeAmount > 0)
        {
            Debug.Log("ForceAutoDosing: Starting auto-dosing process directly");
            
            // Get the ideal amount and how much we need to add
            float idealAmount = (config as GrammingMachineConfig)?.idealGramAmount ?? 18f;
            float currentAmount = service.PortafilterCoffeeAmount;
            float amountNeeded = idealAmount - currentAmount;
            
            if (amountNeeded > 0)
            {
                Debug.Log($"ForceAutoDosing: Portafilter needs {amountNeeded}g more coffee. Starting processing.");
                
                // Start processing
                service.StartProcessing();
                
                // Trigger the auto-dosing effect
                if (processingParticles != null && !processingParticles.isPlaying)
                {
                    processingParticles.Play();
                }
                
                if (processStartSound != null)
                {
                    processStartSound.Play();
                }
                
                // Use the proper timing from config
                float processingTime = (config as GrammingMachineConfig)?.level1AutoDoseTime ?? 2.0f;
                
                // Update progress during the animation
                float startTime = Time.time;
                while (Time.time < startTime + processingTime)
                {
                    float progress = (Time.time - startTime) / processingTime;
                    service.UpdateProgress(progress);
                    yield return null;
                }
                
                // Perform the auto dose
                service.PerformAutoDose();
                
                // Complete processing
                if (processingParticles != null)
                {
                    processingParticles.Stop();
                }
                
                if (processCompleteSound != null)
                {
                    processCompleteSound.Play();
                }
                
                // Update visual
                if (currentPortafilter != null)
                {
                    currentPortafilter.Clear();
                    currentPortafilter.TryAddItem("ground_coffee", service.PortafilterCoffeeAmount);
                }
            }
            else
            {
                Debug.Log("ForceAutoDosing: Portafilter already has enough coffee");
            }
        }
        else
        {
            Debug.Log("ForceAutoDosing: Conditions not met for auto-dosing");
        }
    }
    
    private void StopParticles()
    {
        if (processingParticles != null)
        {
            processingParticles.Stop();
        }
    }
    
    /// <summary>
    /// Called when a portafilter is dropped in the portafilter zone
    /// </summary>
    public void OnPortafilterDropped(Draggable item)
    {
        Debug.Log($"OnPortafilterDropped called with {item?.name}");
        if (item is Portafilter portafilter)
        {
            Debug.Log("Item is a valid portafilter, setting in service");
            currentPortafilter = portafilter;
            
            // Tell the service a portafilter is now present
            service.SetPortafilterPresent(true);
            Debug.Log($"Service now has portafilter: {service.HasPortafilter}");
            
            // Force update the button immediately
            ConfigureGrammingButton();
            
            // Update the UI to reflect portafilter presence
            UpdatePortafilterDisplay(service.PortafilterCoffeeAmount);
            
            // If at level 2 and there's ground coffee, this should trigger auto-operation
            if (service.UpgradeLevel == 2 && service.StoredCoffeeAmount > 0)
            {
                Debug.Log("OnPortafilterDropped: Level 2 with coffee in storage - DIRECTLY triggering auto-dosing");
                
                // Use a small delay to allow UI to update
                Invoke("DirectAutoDosingCheck", 0.5f);
            }
        }
    }
    
    /// <summary>
    /// Called when a portafilter is removed from the portafilter zone
    /// </summary>
    public void OnPortafilterRemoved(Draggable item)
    {
        if (item is Portafilter)
        {
            currentPortafilter = null;
            service.SetPortafilterPresent(false);
        }
    }
    
    private void UpdateUI()
    {
        UpdateStorageDisplay(service.StoredCoffeeAmount);
        UpdatePortafilterDisplay(service.PortafilterCoffeeAmount);
    }
    
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
            if (service.HasPortafilter || currentPortafilter != null)
            {
                portafilterGramText.text = $"{amount:F1}g";
            }
            else
            {
                portafilterGramText.text = "No Portafilter";
            }
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
    
    protected override void HandleUpgradeApplied(int level)
    {
        base.HandleUpgradeApplied(level);
        
        // Update UI based on upgrade level
        UpdateUIForUpgradeLevel(level);
    }
    
    /// <summary>
    /// Updates the UI elements based on the current upgrade level
    /// </summary>
    private void UpdateUIForUpgradeLevel(int level)
    {
        Debug.Log($"CoffeeGrammingMachine: Updating UI for upgrade level {level}");
        
        // Update UI based on upgrade level
        switch (level)
        {
            case 0: // Manual - Hold button
                if (grammingButton != null)
                    grammingButton.gameObject.SetActive(true);
                    
                if (autoDoseButton != null)
                    autoDoseButton.gameObject.SetActive(false);
                    
                if (autoDosingIndicator != null)
                    autoDosingIndicator.gameObject.SetActive(false);
                break;
                
            case 1: // Semi-Auto - Single button press
                if (grammingButton != null)
                    grammingButton.gameObject.SetActive(false);
                    
                if (autoDoseButton != null)
                {
                    autoDoseButton.gameObject.SetActive(true);
                    UpdateAutoDoseButtonState(); // Update interactability
                }
                
                if (autoDosingIndicator != null)
                    autoDosingIndicator.gameObject.SetActive(false);
                break;
                
            case 2: // Fully Automatic
                if (grammingButton != null)
                    grammingButton.gameObject.SetActive(false);
                    
                if (autoDoseButton != null)
                    autoDoseButton.gameObject.SetActive(false);
                    
                if (autoDosingIndicator != null)
                    autoDosingIndicator.gameObject.SetActive(true);
                break;
                
            default:
                Debug.LogWarning($"Unexpected upgrade level: {level}");
                break;
        }
    }
}
