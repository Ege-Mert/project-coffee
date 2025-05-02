using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Espresso machine for brewing coffee
/// </summary>
public class EspressoMachine : MonoBehaviour
{
    [System.Serializable]
    public class BrewingSlot
    {
        public DropZone portafilterZone;
        public DropZone cupZone;
        public GameObject activeIndicator;
        public Image progressFill;
        public Portafilter currentPortafilter;
        public Cup currentCup;
        public bool isActive = false;
        public float brewProgress = 0f;
    }
    
    [Header("References")]
    [SerializeField] private List<BrewingSlot> brewingSlots = new List<BrewingSlot>();
    [SerializeField] private BrewButton brewButton;
    
    [Header("Configuration")]
    [SerializeField] private EspressoMachineConfig config;
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem brewingParticles;
    [SerializeField] private AudioSource brewStartSound;
    [SerializeField] private AudioSource brewEndSound;
    [SerializeField] private AudioSource brewingSound;
    [SerializeField] private Animator machineAnimator;
    
    private int currentUpgradeLevel = 0;
    private float currentBrewTime;
    
    private void Awake()
    {
        // Validate config
        if (config == null)
        {
            Debug.LogError("EspressoMachineConfig is not assigned! Using default values.");
            currentBrewTime = 5f; // Default brew time
        }
        else
        {
            currentBrewTime = config.level0BrewTime;
        }
    }
    
    private void Start()
    {
        // Set up slots
        foreach (BrewingSlot slot in brewingSlots)
        {
            if (slot.activeIndicator != null)
            {
                slot.activeIndicator.SetActive(false);
            }
            
            if (slot.portafilterZone != null)
            {
                // Configure portafilter drop zone
                slot.portafilterZone.AcceptPredicate = (item) => 
                !slot.isActive && item is Portafilter && slot.currentPortafilter == null;
                
                // For simulation only, in a real implementation use event delegates
                // Connect the drop zone's OnDrop to our handler in the Inspector
            }
            
            if (slot.cupZone != null)
            {
                // Configure cup drop zone
                slot.cupZone.AcceptPredicate = (item) => 
                !slot.isActive && item is Cup && slot.currentCup == null;
                
                // For simulation only, in a real implementation use event delegates
                // Connect the drop zone's OnDrop to our handler in the Inspector
            }
            
            if (slot.progressFill != null)
            {
                slot.progressFill.fillAmount = 0f;
            }
        }
        
        // brewButton.CanInteractCheck = () => {
        //     Debug.Log("Checking if brew button can be interacted with...");
        //     bool canInteract = false;
        //     for (int i = 0; i < brewingSlots.Count; i++) {
        //         bool slotCanBrew = CanBrewSlot(i);
        //         Debug.Log($"Slot {i} can brew: {slotCanBrew}");
        //         if (slotCanBrew) canInteract = true;
        //     }
        //     Debug.Log($"Brew button interactable: {canInteract}");
        //     return canInteract;
        // };

        brewButton.CanInteractCustomCheck = () => {
            Debug.Log("Checking if brew button can be interacted with...");
            bool canInteract = false;
            for (int i = 0; i < brewingSlots.Count; i++) {
                bool slotCanBrew = CanBrewSlot(i);
                Debug.Log($"Slot {i} can brew: {slotCanBrew}");
                if (slotCanBrew) canInteract = true;
            }
            Debug.Log($"Brew button interactable: {canInteract}");
            return canInteract;
        };
        
        if (brewingParticles != null)
        {
            brewingParticles.Stop();
        }
        
        if (brewingSound != null)
        {
            brewingSound.Stop();
        }
    }
    
    private void Update()
    {
        // Update brewing slots
        for (int i = 0; i < brewingSlots.Count; i++)
        {
            BrewingSlot slot = brewingSlots[i];
            
            // Skip if not active
            if (!slot.isActive)
                continue;
                
            // Update brewing progress
            slot.brewProgress += Time.deltaTime / currentBrewTime;
            
            // Update progress fill
            if (slot.progressFill != null)
            {
                slot.progressFill.fillAmount = slot.brewProgress;
            }
            
            if (slot.brewProgress >= 1f)
            {
                // Complete brewing
                CompleteBrewing(i);
            }
        }
        
        // Check for items in slots (this is a simplistic approach)
        for (int i = 0; i < brewingSlots.Count; i++)
        {
            BrewingSlot slot = brewingSlots[i];
            
            // Skip if active
            if (slot.isActive)
                continue;
                
            // Check for portafilter
            if (slot.portafilterZone != null && slot.portafilterZone.transform.childCount > 0 && slot.currentPortafilter == null)
            {
                Portafilter portafilter = slot.portafilterZone.transform.GetChild(0).GetComponent<Portafilter>();
                if (portafilter != null)
                {
                    slot.currentPortafilter = portafilter;
                }
            }
            else if (slot.portafilterZone != null && slot.portafilterZone.transform.childCount == 0 && slot.currentPortafilter != null)
            {
                slot.currentPortafilter = null;
            }
            
            // Check for cup
            if (slot.cupZone != null && slot.cupZone.transform.childCount > 0 && slot.currentCup == null)
            {
                Cup cup = slot.cupZone.transform.GetChild(0).GetComponent<Cup>();
                if (cup != null)
                {
                    slot.currentCup = cup;
                }
            }
            else if (slot.cupZone != null && slot.cupZone.transform.childCount == 0 && slot.currentCup != null)
            {
                slot.currentCup = null;
            }
        }

        // In Update() method, add this check
        for (int i = 0; i < brewingSlots.Count; i++) {
            BrewingSlot slot = brewingSlots[i];
            if (slot.isActive && slot.currentCup == null) {
                Debug.LogWarning($"Active brewing slot {i} lost its cup reference!");
                CompleteBrewing(i); // Force completion
            }
        }
    }
    
    // Called by the brew button
    public void OnBrewButtonClick()
    {
        Debug.Log("Brew button clicked!");
        bool anySlotStarted = false;
        
        // Start brewing for any ready slots
        for (int i = 0; i < brewingSlots.Count; i++)
        {
            Debug.Log($"Checking slot {i} for brewing...");
            if (CanBrewSlot(i))
            {
                Debug.Log($"Starting brewing on slot {i}");
                StartBrewing(i);
                anySlotStarted = true;
            }
            else
            {
                Debug.Log($"Slot {i} not ready for brewing");
            }
        }
        
        if (!anySlotStarted)
        {
            Debug.Log("No slots ready for brewing!");
            UIManager.Instance.ShowNotification("No slots ready for brewing!");
        }
    }
    
    private bool CanBrewSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= brewingSlots.Count)
        {
            Debug.Log($"CanBrewSlot: Invalid slot index {slotIndex}");
            return false;
        }
        
        BrewingSlot slot = brewingSlots[slotIndex];
        
        bool hasPortafilter = slot.currentPortafilter != null;
        bool hasGroundCoffee = hasPortafilter && slot.currentPortafilter.HasGroundCoffee;
        bool hasCup = slot.currentCup != null;
        bool notActive = !slot.isActive;
        
        Debug.Log($"Slot {slotIndex} status: HasPortafilter={hasPortafilter}, HasGroundCoffee={hasGroundCoffee}, HasCup={hasCup}, NotActive={notActive}");
        
        return notActive && hasPortafilter && hasGroundCoffee && hasCup;
    }
    
    private void StartBrewing(int slotIndex)
    {
        if (!CanBrewSlot(slotIndex))
        {
            return;
        }
        
        BrewingSlot slot = brewingSlots[slotIndex];
        slot.isActive = true;
        slot.brewProgress = 0f;
        
        if (slot.activeIndicator != null)
        {
            slot.activeIndicator.SetActive(true);
        }
        
        if (slot.progressFill != null)
        {
            slot.progressFill.fillAmount = 0f;
        }
        
        // Visual/audio feedback
        if (brewingParticles != null)
        {
            brewingParticles.transform.position = slot.cupZone.transform.position;
            brewingParticles.Play();
        }
        
        if (brewStartSound != null)
        {
            brewStartSound.Play();
        }
        
        if (brewingSound != null)
        {
            brewingSound.Play();
        }
        
        if (machineAnimator != null)
        {
            machineAnimator.SetTrigger("StartBrew");
        }
        
        UIManager.Instance.ShowNotification("Brewing espresso...");
    }
    
    private void CompleteBrewing(int slotIndex)
    {
        BrewingSlot slot = brewingSlots[slotIndex];
        
        Debug.Log($"CompleteBrewing: Slot {slotIndex}, Cup: {(slot.currentCup != null ? "present" : "missing")}, Portafilter: {(slot.currentPortafilter != null ? "present" : "missing")}");
        
        if (slot.currentCup != null && slot.currentPortafilter != null)
        {
            // Get the quality factor from the portafilter
            float qualityFactor = slot.currentPortafilter.GetCoffeeQualityFactor();
            
            // Get shot amount from config or use default
            float shotAmount = config != null ? 2f : 2f; // Using 2oz as default
            
            // Add espresso to the cup with quality adjusted amount
            float adjustedAmount = shotAmount * Mathf.Lerp(0.7f, 1.2f, qualityFactor);
            Debug.Log($"CompleteBrewing: Adding {adjustedAmount} oz of espresso to cup");
            
            bool success = slot.currentCup.TryAddItem("espresso", adjustedAmount);
            Debug.Log($"Cup.TryAddItem returned: {success}");
            
            // Empty the portafilter
            slot.currentPortafilter.Clear();
            
            // Audio feedback
            if (brewEndSound != null)
            {
                brewEndSound.Play();
            }
            
            if (brewingSound != null)
            {
                brewingSound.Stop();
            }
            
            // Use quality factor for notification
            string qualityDescription = GetQualityDescription(qualityFactor);
            UIManager.Instance.ShowNotification($"{qualityDescription} espresso ready!");
        }
        else
        {
            Debug.LogWarning($"CompleteBrewing: Missing cup or portafilter in slot {slotIndex}");
        }
        
        // Reset the slot
        slot.isActive = false;
        slot.brewProgress = 0f;
        
        if (slot.activeIndicator != null)
        {
            slot.activeIndicator.SetActive(false);
        }
        
        if (brewingParticles != null)
        {
            brewingParticles.Stop();
        }
        
        if (machineAnimator != null)
        {
            machineAnimator.SetTrigger("StopBrew");
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
    
    // Methods to be connected in the Inspector for drop zones
    
    // Called by portafilter drop zone
    public void OnPortafilterDropped(int slotIndex, Draggable item)
    {
        if (slotIndex >= 0 && slotIndex < brewingSlots.Count && item is Portafilter portafilter)
        {
            brewingSlots[slotIndex].currentPortafilter = portafilter;
            
            UIManager.Instance.ShowNotification("Portafilter placed in espresso machine");
        }
    }
    
    // Called by portafilter drop zone
    public void OnPortafilterRemoved(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < brewingSlots.Count)
        {
            brewingSlots[slotIndex].currentPortafilter = null;
        }
    }
    
    // Called by cup drop zone
    public void OnCupDropped(int slotIndex, Draggable item)
    {
        if (slotIndex >= 0 && slotIndex < brewingSlots.Count && item is Cup cup)
        {
            brewingSlots[slotIndex].currentCup = cup;
            
            UIManager.Instance.ShowNotification("Cup placed in espresso machine");
        }
    }
    
    // Called by cup drop zone
    public void OnCupRemoved(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < brewingSlots.Count)
        {
            brewingSlots[slotIndex].currentCup = null;
        }
    }
    
    /// <summary>
    /// Set the upgrade level of the espresso machine
    /// </summary>
    public void SetUpgradeLevel(int level)
    {
        if (config == null) return;
        
        currentUpgradeLevel = level;
        
        // Apply level-specific settings
        switch (level)
        {
            case 0:
                currentBrewTime = config.level0BrewTime;
                break;
                
            case 1:
                currentBrewTime = config.level1BrewTime;
                break;
                
            case 2:
                currentBrewTime = config.level2BrewTime;
                
                // Add additional slots if needed
                if (brewingSlots.Count == 2 && config.level2ExtraSlots > 0)
                {
                    // This would be handled differently in a real implementation
                    // through prefab instantiation or enabling existing slots
                    Debug.Log("Level 2 upgrade unlocks additional brewing slots!");
                }
                break;
        }
        
        Debug.Log($"Espresso machine upgraded to level {level}. New brew time: {currentBrewTime}s");
    }
}