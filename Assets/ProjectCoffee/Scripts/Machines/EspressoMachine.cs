using UnityEngine;
using UnityEngine.UI;
using ProjectCoffee.Services;
using ProjectCoffee.Services.Interfaces;
using ProjectCoffee.Machines;
using ProjectCoffee.Core.Services;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Refactored espresso machine that uses service pattern for logic
/// </summary>
public class EspressoMachine : Machine<EspressoMachineService, EspressoMachineConfig>
{
    [System.Serializable]
    public class BrewingSlotUI
    {
        public DropZone portafilterZone;
        public DropZone cupZone;
        public GameObject activeIndicator;
        public Image progressFill;
        [HideInInspector] public Portafilter currentPortafilter;
        [HideInInspector] public Cup currentCup;
    }
    
    [Header("Espresso Machine Specific UI")]
    [SerializeField] private List<BrewingSlotUI> brewingSlotUIs = new List<BrewingSlotUI>();
    [SerializeField] private BrewButton brewButton;
    
    private Dictionary<int, Coroutine> brewingCoroutines = new Dictionary<int, Coroutine>();
    
    protected override void InitializeService()
    {
        service = new EspressoMachineService(config as EspressoMachineConfig);
    }
    
    protected override void Start()
    {
        base.Start();
        
        // Setup UI elements
        ConfigureDropZones();
        ConfigureBrewButton();
        
        // Subscribe to espresso-specific events
        if (service != null)
        {
            service.OnSlotStateChanged += HandleSlotStateChanged;
            service.OnSlotProgressChanged += HandleSlotProgressChanged;
            service.OnBrewingCompleted += HandleBrewingCompleted;
        }
        
        UpdateAllSlotVisuals();
    }
    
    private void ConfigureDropZones()
    {
        for (int i = 0; i < brewingSlotUIs.Count; i++)
        {
            BrewingSlotUI slotUI = brewingSlotUIs[i];
            int slotIndex = i; // Capture for closure
            
            if (slotUI.portafilterZone != null)
            {
                slotUI.portafilterZone.AcceptPredicate = (item) => 
                {
                    bool isPortafilter = item is Portafilter;
                    bool slotNotActive = !service.GetSlot(slotIndex)?.isActive ?? true;
                    bool noCurrent = slotUI.currentPortafilter == null;
                    bool canAccept = isPortafilter && slotNotActive && noCurrent;
                    Debug.Log($"Portafilter zone {slotIndex} accept check: {canAccept}");
                    return canAccept;
                };
            }
            
            if (slotUI.cupZone != null)
            {
                slotUI.cupZone.AcceptPredicate = (item) => 
                {
                    bool isCup = item is Cup;
                    bool slotNotActive = !service.GetSlot(slotIndex)?.isActive ?? true;
                    bool noCurrent = slotUI.currentCup == null;
                    bool canAccept = isCup && slotNotActive && noCurrent;
                    Debug.Log($"Cup zone {slotIndex} accept check: {canAccept}");
                    return canAccept;
                };
            }
            
            // Initialize indicators
            if (slotUI.activeIndicator != null)
            {
                slotUI.activeIndicator.SetActive(false);
            }
            
            if (slotUI.progressFill != null)
            {
                slotUI.progressFill.fillAmount = 0f;
            }
        }
    }
    
    private void ConfigureBrewButton()
    {
        if (brewButton != null)
        {
            brewButton.CanInteractCustomCheck = () => service?.CanBrewAnySlot() ?? false;
            brewButton.OnClicked.AddListener(OnBrewButtonClicked);
        }
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        // Unsubscribe from events
        if (service != null)
        {
            service.OnSlotStateChanged -= HandleSlotStateChanged;
            service.OnSlotProgressChanged -= HandleSlotProgressChanged;
            service.OnBrewingCompleted -= HandleBrewingCompleted;
        }
        
        if (brewButton != null)
        {
            brewButton.OnClicked.RemoveListener(OnBrewButtonClicked);
        }
    }
    
    private void Update()
    {
        CheckSlotsPresence();
        
        // Get service from ServiceLocator for more robust access
        var espressoService = ServiceLocator.Instance.GetService<IEspressoMachineService>();
        
        // Let the service update brewing progress
        if (espressoService != null)
        {
            espressoService.UpdateBrewing(Time.deltaTime);
        }
    }
    
    private void CheckSlotsPresence()
    {
        for (int i = 0; i < brewingSlotUIs.Count; i++)
        {
            BrewingSlotUI slotUI = brewingSlotUIs[i];
            
            // Check portafilter presence
            if (slotUI.portafilterZone != null)
            {
                if (slotUI.portafilterZone.transform.childCount > 0 && slotUI.currentPortafilter == null)
                {
                    Portafilter portafilter = slotUI.portafilterZone.transform.GetChild(0).GetComponent<Portafilter>();
                    if (portafilter != null)
                    {
                        slotUI.currentPortafilter = portafilter;
                        service?.SetPortafilter(i, true, portafilter.HasGroundCoffee, portafilter.GetCoffeeQualityFactor());
                    }
                }
                else if (slotUI.portafilterZone.transform.childCount == 0 && slotUI.currentPortafilter != null)
                {
                    slotUI.currentPortafilter = null;
                    service?.SetPortafilter(i, false);
                }
            }
            
            // Check cup presence
            if (slotUI.cupZone != null)
            {
                if (slotUI.cupZone.transform.childCount > 0 && slotUI.currentCup == null)
                {
                    Cup cup = slotUI.cupZone.transform.GetChild(0).GetComponent<Cup>();
                    if (cup != null)
                    {
                        slotUI.currentCup = cup;
                        service?.SetCup(i, true);
                    }
                }
                else if (slotUI.cupZone.transform.childCount == 0 && slotUI.currentCup != null)
                {
                    slotUI.currentCup = null;
                    service?.SetCup(i, false);
                }
            }
        }
    }
    
    private void OnBrewButtonClicked()
    {
        Debug.Log("Brew button clicked!");
        
        // Use service locator for more robust access
        var espressoService = ServiceLocator.Instance.GetService<IEspressoMachineService>();
        espressoService?.BrewAllReadySlots();
    }
    
    // This method is used for backward compatibility with old BrewButton
    public void OnBrewButtonClick()
    {
        OnBrewButtonClicked();
    }
    
    private void HandleSlotStateChanged(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= brewingSlotUIs.Count) return;
        
        BrewingSlotUI slotUI = brewingSlotUIs[slotIndex];
        var espressoService = ServiceLocator.Instance.GetService<IEspressoMachineService>();
        var slot = espressoService?.GetSlot(slotIndex);
        
        if (slot == null) return;
        
        // Update active indicator
        if (slotUI.activeIndicator != null)
        {
            slotUI.activeIndicator.SetActive(slot.isActive);
        }
        
        // Start or stop brewing coroutine
        if (slot.isActive && !brewingCoroutines.ContainsKey(slotIndex))
        {
            if (brewingCoroutines.TryGetValue(slotIndex, out Coroutine existingCoroutine))
            {
                StopCoroutine(existingCoroutine);
            }
            brewingCoroutines[slotIndex] = StartCoroutine(BrewingVisualEffects(slotIndex));
        }
        else if (!slot.isActive && brewingCoroutines.ContainsKey(slotIndex))
        {
            StopCoroutine(brewingCoroutines[slotIndex]);
            brewingCoroutines.Remove(slotIndex);
        }
    }
    
    private void HandleSlotProgressChanged(int slotIndex, float progress)
    {
        if (slotIndex < 0 || slotIndex >= brewingSlotUIs.Count) return;
        
        BrewingSlotUI slotUI = brewingSlotUIs[slotIndex];
        
        // Update progress fill
        if (slotUI.progressFill != null)
        {
            slotUI.progressFill.fillAmount = progress;
        }
    }
    
    private void HandleBrewingCompleted(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= brewingSlotUIs.Count) return;
        
        BrewingSlotUI slotUI = brewingSlotUIs[slotIndex];
        
        // Get service from ServiceLocator
        var espressoService = ServiceLocator.Instance.GetService<IEspressoMachineService>();
        var slot = espressoService?.GetSlot(slotIndex);
        
        if (slotUI.currentCup != null && slotUI.currentPortafilter != null && slot != null)
        {
        // Get quality factor from the slot
            float qualityFactor = slot.coffeeQuality;
            
            // Get shot amount from config
            float shotAmount = config != null ? 2f : 2f; // Using 2oz as default
            
            // Add espresso to the cup with quality adjusted amount
            float adjustedAmount = shotAmount * Mathf.Lerp(0.7f, 1.2f, qualityFactor);
            Debug.Log($"Adding {adjustedAmount} oz of espresso to cup (quality: {qualityFactor})");
            
            slotUI.currentCup.TryAddItem("espresso", adjustedAmount);
            
            // Empty the portafilter
            slotUI.currentPortafilter.Clear();
            
            // Feedback
            string qualityDescription = GetQualityDescription(qualityFactor);
            UIManager.Instance.ShowNotification($"{qualityDescription} espresso ready!");
        }
        
        // Play completion sound
        if (processCompleteSound != null)
        {
            processCompleteSound.Play();
        }
    }
    
    private IEnumerator BrewingVisualEffects(int slotIndex)
    {
        BrewingSlotUI slotUI = brewingSlotUIs[slotIndex];
        
        // Start effects
        if (processingParticles != null)
        {
            processingParticles.transform.position = slotUI.cupZone.transform.position;
            processingParticles.Play();
        }
        
        if (processStartSound != null)
        {
            processStartSound.Play();
        }
        
        // Get service from ServiceLocator
        var espressoService = ServiceLocator.Instance.GetService<IEspressoMachineService>();
        
        // Continue while the slot is active
        while (espressoService?.GetSlot(slotIndex)?.isActive ?? false)
        {
            yield return null;
        }
        
        // Stop effects
        if (processingParticles != null)
        {
            processingParticles.Stop();
        }
    }
    
    private void UpdateAllSlotVisuals()
    {
        for (int i = 0; i < brewingSlotUIs.Count; i++)
        {
            HandleSlotStateChanged(i);
        }
    }
    
    private string GetQualityDescription(float qualityFactor)
    {
        if (qualityFactor >= 0.9f)
            return "Perfect";
        else if (qualityFactor >= 0.7f)
            return "Excellent";
        else if (qualityFactor >= 0.5f)
            return "Good";
        else if (qualityFactor >= 0.3f)
            return "Okay";
        else
            return "Poor";
    }
    
    // Public methods for drop zones to call
    public void OnPortafilterDropped(int slotIndex, Draggable item)
    {
        if (slotIndex >= 0 && slotIndex < brewingSlotUIs.Count && item is Portafilter portafilter)
        {
            brewingSlotUIs[slotIndex].currentPortafilter = portafilter;
            service?.SetPortafilter(slotIndex, true, portafilter.HasGroundCoffee, portafilter.GetCoffeeQualityFactor());
            
            UIManager.Instance.ShowNotification("Portafilter placed in espresso machine");
        }
    }
    
    public void OnPortafilterRemoved(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < brewingSlotUIs.Count)
        {
            brewingSlotUIs[slotIndex].currentPortafilter = null;
            service?.SetPortafilter(slotIndex, false);
        }
    }
    
    public void OnCupDropped(int slotIndex, Draggable item)
    {
        if (slotIndex >= 0 && slotIndex < brewingSlotUIs.Count && item is Cup cup)
        {
            brewingSlotUIs[slotIndex].currentCup = cup;
            service?.SetCup(slotIndex, true);
            
            UIManager.Instance.ShowNotification("Cup placed in espresso machine");
        }
    }
    
    public void OnCupRemoved(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < brewingSlotUIs.Count)
        {
            brewingSlotUIs[slotIndex].currentCup = null;
            service?.SetCup(slotIndex, false);
        }
    }
    
    protected override void HandleUpgradeApplied(int level)
    {
        base.HandleUpgradeApplied(level);
        
        // Handle slot count changes for level 2
        if (level >= 2 && config is EspressoMachineConfig espressoConfig)
        {
            // This would be implemented to enable/disable slots as needed
            Debug.Log($"Espresso machine upgraded to level {level}. Additional slots may be available.");
        }
    }
}