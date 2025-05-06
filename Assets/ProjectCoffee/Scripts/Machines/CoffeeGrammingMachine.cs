using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectCoffee.Services;
using ProjectCoffee.Machines;
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
    [SerializeField] private TMP_Text portafilterGramText;
    [SerializeField] private TMP_Text storageGramText;
    [SerializeField] private Image qualityIndicator;
    [SerializeField] private Gradient qualityGradient;
    
    private Portafilter currentPortafilter;
    
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
        
        // Subscribe to gramming-specific events
        if (service != null)
        {
            service.OnCoffeeAmountChanged += UpdateStorageDisplay;
            service.OnPortafilterFillChanged += UpdatePortafilterDisplay;
            service.OnQualityEvaluated += UpdateQualityIndicator;
        }
        
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
                bool result = hasPortafilter && hasCoffee;
                Debug.Log($"GrammingButton CanInteract check: HasPortafilter={hasPortafilter}, HasCoffee={hasCoffee}, Storage={service.StoredCoffeeAmount}g, Result={result}");
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
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        // Unsubscribe from events
        if (service != null)
        {
            service.OnCoffeeAmountChanged -= UpdateStorageDisplay;
            service.OnPortafilterFillChanged -= UpdatePortafilterDisplay;
            service.OnQualityEvaluated -= UpdateQualityIndicator;
        }
    }
    
    private void Update()
    {
        CheckPortafilterPresence();
        
        // Check for automatic operation at upgrade level 2
        if (service.UpgradeLevel >= 2)
        {
            service.CheckAutoOperation();
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
            service.SetPortafilterPresent(true);
            Debug.Log($"Service now has portafilter: {service.HasPortafilter}");
            
            // Force update the button immediately
            ConfigureGrammingButton();
            
            // Update the UI to reflect portafilter presence
            UpdatePortafilterDisplay(service.PortafilterCoffeeAmount);
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
        if (grammingButton != null)
        {
            // You might want to change button behavior based on level
            switch (level)
            {
                case 0:
                    // Manual hold operation
                    break;
                case 1:
                    // Single button press for perfect dose
                    // TODO: Implement single-press behavior
                    break;
                case 2:
                    // Automatic operation
                    grammingButton.gameObject.SetActive(false);
                    break;
            }
        }
    }
}
