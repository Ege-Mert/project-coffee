using UnityEngine;
using UnityEngine.UI;
using ProjectCoffee.Services;
using ProjectCoffee.Services.Interfaces;
using ProjectCoffee.Machines;
using ProjectCoffee.Core.Services;
using ProjectCoffee.Interaction.Helpers;
using ProjectCoffee.Machines.Components;
using ProjectCoffee.Utils;
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
        public EspressoMachineDropZone portafilterZone;
        public EspressoMachineDropZone cupZone;
        public GameObject activeIndicator;
        public Image progressFill;
        [HideInInspector] public Portafilter currentPortafilter;
        [HideInInspector] public Cup currentCup;
    }
    
    [Header("Espresso Machine Specific UI")]
    [SerializeField] private List<BrewingSlotUI> brewingSlotUIs = new List<BrewingSlotUI>();
    [SerializeField] private BrewButton brewButton;
    [SerializeField] private GameObject autoBrewIndicator;
    [SerializeField] private GameObject manualBrewIndicator;
    
    private Dictionary<int, Coroutine> brewingCoroutines = new Dictionary<int, Coroutine>();
    
    // Add these fields to the class
    private Dictionary<int, float> lastStateUpdateTime = new Dictionary<int, float>();
    private const float STATE_UPDATE_COOLDOWN = 0.1f; // Prevent updates more than 10 times per second
    
    protected override void InitializeService()
    {
        service = new EspressoMachineService(config as EspressoMachineConfig);
    }
    
    protected override void Start()
    {
        base.Start();
        
        // Ensure all drop zones have proper trackers
        InitializeDropZoneTrackers();
        
        // CRITICAL: Ensure all draggable items have state managers before any brewing can start
        EnsureStateManagersOnAllItems();
        
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
        
        // Apply current upgrade level settings
        SetUpgradeLevel(service?.UpgradeLevel ?? 0);
        
        UpdateAllSlotVisuals();
    }
    
    private void InitializeDropZoneTrackers()
    {
        for (int i = 0; i < brewingSlotUIs.Count; i++)
        {
            BrewingSlotUI slotUI = brewingSlotUIs[i];
            
            // Initialize portafilter zone
            if (slotUI.portafilterZone != null)
            {
                // Ensure it has a tracker
                var tracker = slotUI.portafilterZone.GetComponent<DropZoneItemTracker>();
                if (tracker == null)
                {
                    tracker = slotUI.portafilterZone.gameObject.AddComponent<DropZoneItemTracker>();
                    Debug.Log($"Added DropZoneItemTracker to portafilter zone slot {i}");
                }
                
                // Ensure the zone knows its slot index
                Debug.Log($"Initialized portafilter zone for slot {i}: {slotUI.portafilterZone.name}");
            }
            
            // Initialize cup zone
            if (slotUI.cupZone != null)
            {
                // Ensure it has a tracker
                var tracker = slotUI.cupZone.GetComponent<DropZoneItemTracker>();
                if (tracker == null)
                {
                    tracker = slotUI.cupZone.gameObject.AddComponent<DropZoneItemTracker>();
                    Debug.Log($"Added DropZoneItemTracker to cup zone slot {i}");
                }
                
                // Ensure the zone knows its slot index
                Debug.Log($"Initialized cup zone for slot {i}: {slotUI.cupZone.name}");
            }
        }
    }
    
    /// <summary>
    /// Ensure all draggable items have state managers initialized properly
    /// </summary>
    private void EnsureStateManagersOnAllItems()
    {
        // Find all portafilters and cups in the scene
        var allPortafilters = FindObjectsOfType<Portafilter>();
        var allCups = FindObjectsOfType<Cup>();
        
        // Add state managers to portafilters
        foreach (var portafilter in allPortafilters)
        {
            var stateManager = portafilter.GetComponent<DraggableStateManager>();
            if (stateManager == null)
            {
                stateManager = portafilter.gameObject.AddComponent<DraggableStateManager>();
                Debug.Log($"Added DraggableStateManager to portafilter: {portafilter.name}");
            }
        }
        
        // Add state managers to cups
        foreach (var cup in allCups)
        {
            var stateManager = cup.GetComponent<DraggableStateManager>();
            if (stateManager == null)
            {
                stateManager = cup.gameObject.AddComponent<DraggableStateManager>();
                Debug.Log($"Added DraggableStateManager to cup: {cup.name}");
            }
        }
    }
    
    private void ConfigureDropZones()
    {
        for (int i = 0; i < brewingSlotUIs.Count; i++)
        {
            BrewingSlotUI slotUI = brewingSlotUIs[i];
            
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
            if (i >= 2 && upgradeLevel < 2) continue;
            
            BrewingSlotUI slotUI = brewingSlotUIs[i];
            bool portafilterChanged = false;
            bool cupChanged = false;    
            
            // Check if enough time has passed since last update for this slot
            bool canUpdate = !lastStateUpdateTime.ContainsKey(i) || 
                            (Time.time - lastStateUpdateTime[i]) > STATE_UPDATE_COOLDOWN;
            
            // Check portafilter presence
            if (slotUI.portafilterZone != null)
            {
                var tracker = slotUI.portafilterZone.GetComponent<DropZoneItemTracker>();
                if (tracker != null && tracker.HasItem && slotUI.currentPortafilter == null)
                {
                    Portafilter portafilter = tracker.CurrentItem as Portafilter;
                    if (portafilter != null)
                    {
                        slotUI.currentPortafilter = portafilter;
                        if (canUpdate)
                        {
                            service?.SetPortafilter(i, true, portafilter.HasGroundCoffee, portafilter.GetCoffeeQualityFactor());
                            lastStateUpdateTime[i] = Time.time;
                        }
                        portafilterChanged = true;
                        Debug.Log($"Portafilter added to slot {i}: HasGroundCoffee={portafilter.HasGroundCoffee}, Quality={portafilter.GetCoffeeQualityFactor()}");
                    }
                }
                else if (tracker != null && !tracker.HasItem && slotUI.currentPortafilter != null)
                {
                    slotUI.currentPortafilter = null;
                    if (canUpdate)
                    {
                        service?.SetPortafilter(i, false);
                        lastStateUpdateTime[i] = Time.time;
                    }
                    portafilterChanged = true;
                    Debug.Log($"Portafilter removed from slot {i}");
                }
                else if (slotUI.currentPortafilter != null && canUpdate)
                {
                    var espressoService = ServiceLocator.Instance.GetService<IEspressoMachineService>();
                    var slot = espressoService?.GetSlot(i);
                    
                    if (slot != null)
                    {
                        bool currentHasCoffee = slotUI.currentPortafilter.HasGroundCoffee;
                        float currentQuality = slotUI.currentPortafilter.GetCoffeeQualityFactor();
                        
                        if (slot.hasGroundCoffee != currentHasCoffee || 
                            (currentHasCoffee && Mathf.Abs(slot.coffeeQuality - currentQuality) > 0.01f))
                        {
                            service?.SetPortafilter(i, true, currentHasCoffee, currentQuality);
                            lastStateUpdateTime[i] = Time.time;
                            Debug.Log($"Coffee state changed in slot {i}: HasGroundCoffee={currentHasCoffee}, Quality={currentQuality}");
                        }
                    }
                }
            }
            
            // Check cup presence
            if (slotUI.cupZone != null)
            {
                var tracker = slotUI.cupZone.GetComponent<DropZoneItemTracker>();
                if (tracker != null && tracker.HasItem && slotUI.currentCup == null)
                {
                    Cup cup = tracker.CurrentItem as Cup;
                    if (cup != null)
                    {
                        slotUI.currentCup = cup;
                        if (canUpdate)
                        {
                            service?.SetCup(i, true);
                            lastStateUpdateTime[i] = Time.time;
                        }
                        cupChanged = true;
                        Debug.Log($"Cup added to slot {i}");
                    }
                }
                else if (tracker != null && !tracker.HasItem && slotUI.currentCup != null)
                {
                    slotUI.currentCup = null;
                    if (canUpdate)
                    {
                        service?.SetCup(i, false);
                        lastStateUpdateTime[i] = Time.time;
                    }
                    cupChanged = true;
                    Debug.Log($"Cup removed from slot {i}");
                }
            }
            
            // REMOVE ALL AUTO-BREW LOGIC FROM HERE - the service handles it now
            // No more AutoBrewWithDelay or TryAutoBrewIfReady calls
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
        
        // Update item states based on brewing state
        UpdateSlotItemStates(slotIndex, slot.isActive);
        
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
    
    // Update the visual state of items in a slot based on brewing state
    private void UpdateSlotItemStates(int slotIndex, bool isProcessing)
    {
        if (slotIndex < 0 || slotIndex >= brewingSlotUIs.Count) return;
        
        BrewingSlotUI slotUI = brewingSlotUIs[slotIndex];
        
        // REMOVED: Don't set processing state during brewing to prevent transparency
        // We'll only prevent dragging, not change visual appearance
        
        // Instead, just disable dragging without visual changes
        if (slotUI.currentPortafilter != null)
        {
            var portafilterDraggable = slotUI.currentPortafilter.GetComponent<Draggable>();
            if (portafilterDraggable != null)
            {
                portafilterDraggable.enabled = !isProcessing;
            }
        }
        
        if (slotUI.currentCup != null)
        {
            var cupDraggable = slotUI.currentCup.GetComponent<Draggable>();
            if (cupDraggable != null)
            {
                cupDraggable.enabled = !isProcessing;
            }
        }
        
        Debug.Log($"Updated slot {slotIndex} items dragging enabled: {!isProcessing}");
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
            float shotAmount = config != null ? 2f : 2f;
            
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
        
        // IMMEDIATELY restore dragging - do this FIRST
        RestoreSlotItemsToNormalState(slotIndex);
        
        // ADDITIONAL FIX: Add a delayed restoration to ensure everything is properly restored
        StartCoroutine(DelayedRestoreSlotItems(slotIndex));
        
        // Add a delay before allowing state updates again
        StartCoroutine(ResetStateUpdateCooldown(slotIndex));
    }
    
    private IEnumerator DelayedRestoreSlotItems(int slotIndex)
    {
        // Wait a few frames to ensure all state changes have propagated
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f);
        
        Debug.Log($"[DelayedRestoreSlotItems] Running delayed restoration for slot {slotIndex}");
        
        // Force restore again
        RestoreSlotItemsToNormalState(slotIndex);
        
        // Add debug check
        if (slotIndex < brewingSlotUIs.Count)
        {
            BrewingSlotUI slotUI = brewingSlotUIs[slotIndex];
            
            if (slotUI.currentPortafilter != null)
            {
                var debugger = slotUI.currentPortafilter.GetComponent<DraggableDebugger>();
                if (debugger == null)
                {
                    debugger = slotUI.currentPortafilter.gameObject.AddComponent<DraggableDebugger>();
                }
                // debugger.ForceCheck();
            }
            
            if (slotUI.currentCup != null)
            {
                var debugger = slotUI.currentCup.GetComponent<DraggableDebugger>();
                if (debugger == null)
                {
                    debugger = slotUI.currentCup.gameObject.AddComponent<DraggableDebugger>();
                }
                // debugger.ForceCheck();
            }
        }
    }

    private IEnumerator ResetStateUpdateCooldown(int slotIndex)
    {
        yield return new WaitForSeconds(0.5f); // Give some time for everything to settle
        if (lastStateUpdateTime.ContainsKey(slotIndex))
        {
            lastStateUpdateTime[slotIndex] = 0f; // Reset cooldown
        }
    }
    
    // Updated method to properly restore item states without visual effects
    private void RestoreSlotItemsToNormalState(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= brewingSlotUIs.Count) return;
        
        BrewingSlotUI slotUI = brewingSlotUIs[slotIndex];
        
        // Restore portafilter state - force enable dragging and clear any processing state
        if (slotUI.currentPortafilter != null)
        {
            Debug.Log($"Restoring portafilter state for slot {slotIndex}: {slotUI.currentPortafilter.name}");
            
            // Force enable dragging - do this FIRST and ALWAYS
            var portafilterDraggable = slotUI.currentPortafilter.GetComponent<Draggable>();
            if (portafilterDraggable != null)
            {
                portafilterDraggable.enabled = true;
                portafilterDraggable.RefreshCanvasReference(); // CRITICAL: Refresh canvas reference
                Debug.Log($"Enabled dragging for portafilter: {slotUI.currentPortafilter.name}");
            }
            
            // CRITICAL FIX: Ensure CanvasGroup allows raycasts
            var portafilterCanvasGroup = slotUI.currentPortafilter.GetComponent<CanvasGroup>();
            if (portafilterCanvasGroup != null)
            {
                portafilterCanvasGroup.blocksRaycasts = true;
                portafilterCanvasGroup.interactable = true;
                portafilterCanvasGroup.alpha = 1f;
                Debug.Log($"Restored CanvasGroup settings for portafilter: {slotUI.currentPortafilter.name}");
            }
            
            // Clear any processing visual state
            var portafilterStateManager = slotUI.currentPortafilter.GetComponent<DraggableStateManager>();
            if (portafilterStateManager != null)
            {
                portafilterStateManager.ForceReset();
                Debug.Log($"Force reset state manager for portafilter: {slotUI.currentPortafilter.name}");
            }
            
            // DOUBLE-CHECK: Ensure dragging is still enabled after state manager reset
            if (portafilterDraggable != null && !portafilterDraggable.enabled)
            {
                Debug.LogWarning($"Portafilter dragging was disabled after state reset! Force enabling again.");
                portafilterDraggable.enabled = true;
                portafilterDraggable.RefreshCanvasReference();
            }
        }
        
        // Restore cup state - force enable dragging and clear any processing state
        if (slotUI.currentCup != null)
        {
            Debug.Log($"Restoring cup state for slot {slotIndex}: {slotUI.currentCup.name}");
            
            // Force enable dragging - do this FIRST and ALWAYS
            var cupDraggable = slotUI.currentCup.GetComponent<Draggable>();
            if (cupDraggable != null)
            {
                cupDraggable.enabled = true;
                cupDraggable.RefreshCanvasReference(); // CRITICAL: Refresh canvas reference
                Debug.Log($"Enabled dragging for cup: {slotUI.currentCup.name}");
            }
            else
            {
                Debug.LogWarning($"No Draggable component found on cup: {slotUI.currentCup.name}");
            }
            
            // CRITICAL FIX: Ensure CanvasGroup allows raycasts
            var cupCanvasGroup = slotUI.currentCup.GetComponent<CanvasGroup>();
            if (cupCanvasGroup != null)
            {
                cupCanvasGroup.blocksRaycasts = true;
                cupCanvasGroup.interactable = true;
                cupCanvasGroup.alpha = 1f;
                Debug.Log($"Restored CanvasGroup settings for cup: {slotUI.currentCup.name}");
            }
            
            // Clear any processing visual state
            var cupStateManager = slotUI.currentCup.GetComponent<DraggableStateManager>();
            if (cupStateManager != null)
            {
                cupStateManager.ForceReset();
                Debug.Log($"Force reset state manager for cup: {slotUI.currentCup.name}");
            }
            else
            {
                Debug.LogWarning($"No DraggableStateManager component found on cup: {slotUI.currentCup.name}");
            }
            
            // DOUBLE-CHECK: Ensure dragging is still enabled after state manager reset
            if (cupDraggable != null && !cupDraggable.enabled)
            {
                Debug.LogWarning($"Cup dragging was disabled after state reset! Force enabling again.");
                cupDraggable.enabled = true;
                cupDraggable.RefreshCanvasReference();
            }
            
            // Additional fix: Ensure the cup's RectTransform is not locked
            var cupRect = slotUI.currentCup.GetComponent<RectTransform>();
            if (cupRect != null)
            {
                // Reset any transform locks that might have been applied
                cupRect.localScale = Vector3.one;
                Debug.Log($"Reset transform for cup: {slotUI.currentCup.name}");
            }
        }
        
        Debug.Log($"Force restored normal state for items in slot {slotIndex}");
    }
    
    private IEnumerator BrewingVisualEffects(int slotIndex)
    {
        BrewingSlotUI slotUI = brewingSlotUIs[slotIndex];
        
        var espressoService = ServiceLocator.Instance.GetService<IEspressoMachineService>();
        
        while (espressoService?.GetSlot(slotIndex)?.isActive ?? false)
        {
            yield return null;
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
        upgradeLevel = level;
        
        // Update UI for new level
        UpdateBrewUIForLevel(level);
        
        // Handle slot count changes for level 2
        if (level >= 2 && config is EspressoMachineConfig espressoConfig)
        {
            // Enable additional slots
            EnableAdditionalSlots(espressoConfig.level2ExtraSlots);
            Debug.Log($"Espresso machine upgraded to level {level}. Additional slots available: {espressoConfig.level2ExtraSlots}");
        }
    }
    
    // Store the current upgrade level for reference
    private int upgradeLevel = 0;
    
    // Update UI elements based on upgrade level
    private void UpdateBrewUIForLevel(int level)
    {
        if (autoBrewIndicator != null)
        {
            // Only show auto-brew indicator at level 2
            autoBrewIndicator.SetActive(level == 2);
        }
        
        if (manualBrewIndicator != null)
        {
            // Only show manual brew indicator below level 2
            manualBrewIndicator.SetActive(level < 2);
        }
        
        if (brewButton != null)
        {
            // At level 2, hide the brew button completely as brewing is automatic
            brewButton.gameObject.SetActive(level < 2);
        }
        
        // Update the appearance of all slots for the new level
        UpdateAllSlotVisuals();
    }
    
    // Enable or disable additional slots based on upgrade level
    private void EnableAdditionalSlots(int extraSlotCount)
    {
        // We only have extra slots at level 2
        bool isLevel2 = upgradeLevel == 2;
        
        // Enable all slots up to the current level's maximum
        for (int i = 0; i < brewingSlotUIs.Count; i++)
        {
            // Only first 2 slots are active before level 2
            bool shouldBeActive = i < 2 || isLevel2;
            
            // Activate or deactivate the slot UI
            if (i < brewingSlotUIs.Count)
            {
                // Find the root GameObject for this slot (might be a parent of the zones)
                var slotUI = brewingSlotUIs[i];
                Transform slotRoot = null;
                
                if (slotUI.portafilterZone != null) slotRoot = slotUI.portafilterZone.transform.parent;
                else if (slotUI.cupZone != null) slotRoot = slotUI.cupZone.transform.parent;
                
                if (slotRoot != null)
                {
                    slotRoot.gameObject.SetActive(shouldBeActive);
                }
                else
                {
                    // Fallback if we can't find a parent: enable/disable the individual zones
                    if (slotUI.portafilterZone != null) slotUI.portafilterZone.gameObject.SetActive(shouldBeActive);
                    if (slotUI.cupZone != null) slotUI.cupZone.gameObject.SetActive(shouldBeActive);
                    if (slotUI.activeIndicator != null) slotUI.activeIndicator.gameObject.SetActive(shouldBeActive && slotUI.activeIndicator.activeSelf);
                }
            }
        }
        
        Debug.Log($"Updated slot visibility: First 2 slots always visible, slots 3-4 visible: {isLevel2}");
    }
    
    private void HandleSlotProgressChanged(int slotIndex, float progress)
    {
        if (slotIndex < 0 || slotIndex >= brewingSlotUIs.Count) return;
        
        BrewingSlotUI slotUI = brewingSlotUIs[slotIndex];
        
        // Update progress fill UI
        if (slotUI.progressFill != null)
        {
            slotUI.progressFill.fillAmount = progress;
        }
        
        // Optional: Add visual feedback based on progress
        if (progress > 0f && progress < 1f)
        {
            // Brewing in progress - could add additional visual effects here
            Debug.Log($"Slot {slotIndex} brewing progress: {progress:P0}");
        }
    }
}