using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Coffee gramming machine for measuring and dispensing ground coffee
/// </summary>
public class CoffeeGrammingMachine : MonoBehaviour, ICoffeeContainer
{
    [Header("References")]
    [SerializeField] private DropZone portafilterZone;
    [SerializeField] private DropZone groundCoffeeZone;
    [SerializeField] private Holdable grammingButton;
    [SerializeField] private TMP_Text portafilterGramText;
    [SerializeField] private TMP_Text storageGramText;
    [SerializeField] private Image qualityIndicator;
    [SerializeField] private Gradient qualityGradient;
    
    [Header("Configuration")]
    [SerializeField] private GrammingMachineConfig config;
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem coffeeParticles;
    [SerializeField] private AudioSource coffeeAddSound;
    [SerializeField] private AudioSource coffeeDispenseSound;
    [SerializeField] private Animator machineAnimator;

    [Header("Debug")]
    [SerializeField] private bool logDebugging = true;
    
    // State variables
    private Portafilter currentPortafilter;
    private float storedCoffeeAmount = 0f; // Internal coffee storage
    private CoffeeQualityEvaluator qualityEvaluator;
    private bool isDispensing = false;
    private int currentUpgradeLevel = 0;
    
    private void Awake()
    {
        // Validate config
        if (config == null)
        {
            Debug.LogError("GrammingMachineConfig is not assigned! Using default values.");
            qualityEvaluator = new CoffeeQualityEvaluator(18f, 1f); // Default values
        }
        else
        {
            qualityEvaluator = new CoffeeQualityEvaluator(config.idealGramAmount, config.gramTolerance);
        }
    }
    
    private void Start()
    {
        DebugLog("CoffeeGrammingMachine Start called");
        
        // Add test coffee for debugging
        storedCoffeeAmount = 50f;
        
        ConfigureDropZones();
        ConfigureGrammingButton();
        
        if (coffeeParticles != null)
        {
            coffeeParticles.Stop();
        }
        
        UpdateUI();
    }
    
    private void ConfigureDropZones()
    {
        if (portafilterZone != null)
        {
            DebugLog($"Configuring portafilter zone: {portafilterZone.name}");
            portafilterZone.AcceptPredicate = (item) => item is Portafilter && currentPortafilter == null;
        }
        else
        {
            Debug.LogError("Portafilter zone reference is missing!");
        }
        
        if (groundCoffeeZone != null)
        {
            DebugLog($"Configuring ground coffee zone: {groundCoffeeZone.name}");
            // Accept ground coffee regardless of portafilter presence
            groundCoffeeZone.AcceptPredicate = (item) => item is GroundCoffee;
        }
        else
        {
            Debug.LogError("Ground coffee zone reference is missing!");
        }
    }
    
    private void ConfigureGrammingButton()
    {
        if (grammingButton != null)
        {
            DebugLog($"Configuring gramming button: {grammingButton.name}");
            
            // Directly assign the method reference
            grammingButton.CanInteract = () => {
                bool canInteract = currentPortafilter != null && storedCoffeeAmount > 0;
                DebugLog($"Gramming button CanInteract check: {canInteract} (Portafilter: {(currentPortafilter != null)}, Coffee: {storedCoffeeAmount}g)");
                return canInteract;
            };
            
            grammingButton.OnHold = OnGrammingButtonHold;
            grammingButton.OnHoldRelease = OnGrammingButtonRelease;
        }
        else
        {
            Debug.LogError("Gramming button reference is missing!");
        }
    }
    
    private void Update()
    {
        CheckPortafilterPresence();
        
        // Auto-dose at level 2 if conditions are met
        if (currentUpgradeLevel == 2 && config != null)
        {
            // Level 2 auto-detection logic will be implemented here
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
                DebugLog($"Portafilter detected: {portafilter.name}");
                currentPortafilter = portafilter;
                UpdateUI();
                UIManager.Instance.ShowNotification("Portafilter placed in gramming machine");
                
                // Auto-dose at level 1 if conditions are met
                if (currentUpgradeLevel == 1 && config != null && storedCoffeeAmount > 0 && !isDispensing)
                {
                    // Will be implemented later - level 1 auto dose
                }
            }
        }
        // Detect when portafilter is removed
        else if (portafilterZone.transform.childCount == 0 && currentPortafilter != null)
        {
            DebugLog("Portafilter removed");
            currentPortafilter = null;
            UpdateUI();
        }
    }
    
    private void OnGrammingButtonHold(float duration)
    {
        DebugLog($"Gramming button held for {duration}s");
        
        if (currentPortafilter == null)
        {
            DebugLog("Cannot dispense: No portafilter");
            return;
        }
        
        if (storedCoffeeAmount <= 0)
        {
            DebugLog("Cannot dispense: No stored coffee");
            return;
        }
        
        // Get rate from config or use default
        float rate = config != null ? config.grammingRate : 2f;
        
        // Calculate amount to dispense
        float amountToDispense = rate * Time.deltaTime;
        amountToDispense = Mathf.Min(amountToDispense, storedCoffeeAmount);
        
        if (amountToDispense > 0)
        {
            if (!isDispensing)
            {
                isDispensing = true;
                DebugLog("Started dispensing coffee");
                PlayDispenseEffects();
            }
            
            // Remove from storage
            storedCoffeeAmount -= amountToDispense;
            
            // Add to portafilter
            bool added = currentPortafilter.TryAddItem("ground_coffee", amountToDispense);
            DebugLog($"Added {amountToDispense}g of coffee to portafilter, success: {added}");
            
            // Update UI
            UpdateUI();
        }
        else if (storedCoffeeAmount <= 0)
        {
            // Stop effects if we're out of coffee
            StopDispenseEffects();
            UIManager.Instance.ShowNotification("Out of ground coffee!");
        }
    }
    
    private void OnGrammingButtonRelease(float heldDuration)
    {
        DebugLog($"Gramming button released after {heldDuration}s");
        isDispensing = false;
        StopDispenseEffects();
        
        // Provide feedback on current coffee quality
        if (currentPortafilter != null)
        {
            float coffeeAmount = currentPortafilter.GetItemAmount("ground_coffee");
            float quality = qualityEvaluator.EvaluateQuality(coffeeAmount);
            string qualityDesc = qualityEvaluator.GetQualityDescription(quality);
            
            DebugLog($"Coffee in portafilter: {coffeeAmount}g, Quality: {qualityDesc} ({quality:F2})");
            
            // Only show notification if we actually dispensed some coffee
            if (heldDuration > 0.1f && coffeeAmount > 0)
            {
                UIManager.Instance.ShowNotification($"{qualityDesc} coffee weight: {coffeeAmount:F1}g");
            }
        }
    }
    
    #region ICoffeeContainer Implementation
    
    /// <summary>
    /// Add coffee to the machine's internal storage
    /// </summary>
    public bool TryAddCoffee(float amount)
    {
        if (amount <= 0)
            return false;
            
        // Get capacity from config or use default
        float maxCapacity = config != null ? config.maxStorageCapacity : 100f;
            
        // Check if adding would exceed capacity
        if (storedCoffeeAmount + amount > maxCapacity)
        {
            float actualAmount = maxCapacity - storedCoffeeAmount;
            storedCoffeeAmount = maxCapacity;
            
            UpdateUI();
            UIManager.Instance.ShowNotification($"Added {actualAmount:F1}g to machine storage. Storage full!");
            return false; // Couldn't add full amount
        }
        
        storedCoffeeAmount += amount;
        UpdateUI();
        UIManager.Instance.ShowNotification($"Added {amount:F1}g to machine storage. Total: {storedCoffeeAmount:F1}g");
        return true;
    }
    
    public bool TryRemoveCoffee(float amount)
    {
        if (amount <= 0 || storedCoffeeAmount < amount)
            return false;
            
        storedCoffeeAmount -= amount;
        UpdateUI();
        return true;
    }
    
    public float GetCoffeeAmount()
    {
        return storedCoffeeAmount;
    }
    
    public bool HasCoffee(float minAmount = 0f)
    {
        return storedCoffeeAmount >= minAmount;
    }
    
    #endregion
    
    #region Effects
    
    private void PlayDispenseEffects()
    {
        if (coffeeParticles != null && !coffeeParticles.isPlaying)
        {
            coffeeParticles.Play();
        }
        
        if (coffeeDispenseSound != null && coffeeDispenseSound.isActiveAndEnabled && !coffeeDispenseSound.isPlaying)
        {
            coffeeDispenseSound.Play();
        }
        
        if (machineAnimator != null)
        {
            machineAnimator.SetBool("Dispensing", true);
        }
    }
    
    private void StopDispenseEffects()
    {
        if (coffeeParticles != null)
        {
            coffeeParticles.Stop();
        }
        
        if (coffeeDispenseSound != null && coffeeDispenseSound.isPlaying)
        {
            coffeeDispenseSound.Stop();
        }
        
        if (machineAnimator != null)
        {
            machineAnimator.SetBool("Dispensing", false);
        }
    }
    
    #endregion
    
    #region UI Updates
    
    private void UpdateUI()
    {
        UpdatePortafilterText();
        UpdateStorageText();
        UpdateQualityIndicator();
    }
    
    private void UpdatePortafilterText()
    {
        if (portafilterGramText != null)
        {
            if (currentPortafilter != null)
            {
                float coffeeAmount = currentPortafilter.GetItemAmount("ground_coffee");
                portafilterGramText.text = $"{coffeeAmount:F1}g";
            }
            else
            {
                portafilterGramText.text = "No Portafilter";
            }
        }
    }
    
    private void UpdateStorageText()
    {
        if (storageGramText != null)
        {
            storageGramText.text = $"Storage: {storedCoffeeAmount:F1}g";
        }
    }
    
    private void UpdateQualityIndicator()
    {
        if (qualityIndicator == null) return;
        
        if (currentPortafilter != null)
        {
            float coffeeAmount = currentPortafilter.GetItemAmount("ground_coffee");
            float quality = qualityEvaluator.EvaluateQuality(coffeeAmount);
            qualityIndicator.color = qualityGradient.Evaluate(quality);
            
            float idealAmount = config != null ? config.idealGramAmount : 18f;
            float fillAmount = 0f;
            
            if (coffeeAmount > 0)
            {
                if (coffeeAmount <= idealAmount)
                {
                    fillAmount = coffeeAmount / idealAmount;
                }
                else
                {
                    float overFillRange = idealAmount * 1.5f - idealAmount;
                    float overAmount = coffeeAmount - idealAmount;
                    fillAmount = 1f - (overAmount / overFillRange) * 0.5f;
                }
            }
            
            qualityIndicator.fillAmount = fillAmount;
            qualityIndicator.gameObject.SetActive(true);
        }
        else
        {
            float maxCapacity = config != null ? config.maxStorageCapacity : 100f;
            qualityIndicator.color = Color.white;
            qualityIndicator.fillAmount = storedCoffeeAmount / maxCapacity;
            qualityIndicator.gameObject.SetActive(storedCoffeeAmount > 0);
        }
    }
    
    #endregion
    
    #region DropZone Event Handlers
    
    /// <summary>
    /// Called when a portafilter is dropped in the portafilter zone
    /// </summary>
    public void OnPortafilterDropped(Draggable item)
    {
        if (item is Portafilter portafilter)
        {
            DebugLog($"Portafilter dropped: {portafilter.name}");
            currentPortafilter = portafilter;
            UpdateUI();
            UIManager.Instance.ShowNotification("Portafilter placed in gramming machine");
        }
    }
    
    /// <summary>
    /// Called when a portafilter is removed from the portafilter zone
    /// </summary>
    public void OnPortafilterRemoved(Draggable item)
    {
        if (item is Portafilter)
        {
            DebugLog("Portafilter removed from zone");
            currentPortafilter = null;
            UpdateUI();
        }
    }
    
    /// <summary>
    /// Called when ground coffee is dropped in the ground coffee zone
    /// </summary>
    public void OnGroundCoffeeDropped(Draggable item)
    {
        if (item is GroundCoffee groundCoffee)
        {
            float coffeeAmount = groundCoffee.GetAmount();
            DebugLog($"Ground coffee dropped: {coffeeAmount}g");
            
            // Add to storage instead of directly to portafilter
            TryAddCoffee(coffeeAmount);
            
            // Visual feedback
            if (coffeeParticles != null)
            {
                coffeeParticles.Play();
                Invoke("StopParticles", 1.0f); // Stop particles after a delay
            }
            
            // Sound
            if (coffeeAddSound != null && coffeeAddSound.isActiveAndEnabled)
            {
                coffeeAddSound.Play();
            }
            
            // Animation
            if (machineAnimator != null)
            {
                machineAnimator.SetTrigger("AddCoffee");
            }
            
            DOTween.Kill(groundCoffee.transform);
        
            // Destroy the object after a short delay
            Destroy(groundCoffee.gameObject, 0.5f);
        }
    }
    
    private void StopParticles()
    {
        if (coffeeParticles != null && !isDispensing)
        {
            coffeeParticles.Stop();
        }
    }
    
    #endregion
    
    /// <summary>
    /// Set the upgrade level of the machine
    /// </summary>
    public void SetUpgradeLevel(int level)
    {
        currentUpgradeLevel = level;
        DebugLog($"Machine upgraded to level {level}");
        
        // Additional upgrade-specific logic will be implemented later
    }
    
    private void DebugLog(string message)
    {
        if (logDebugging)
        {
            Debug.Log($"[GrammingMachine] {message}");
        }
    }
}