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
    // Removed the single display field:
    // [SerializeField] private TMP_Text gramDisplayText;
    // Added separate texts for portafilter grammage and storage amount
    [SerializeField] private TMP_Text portafilterGramText;
    [SerializeField] private TMP_Text storageGramText;
    
    [SerializeField] private Image qualityIndicator;
    [SerializeField] private Gradient qualityGradient;
    
    [Header("Settings")]
    [SerializeField] private float grammingRate = 2f; // Grams per second when dispensing
    [SerializeField] private float idealGramAmount = 18f;
    [SerializeField] private float gramTolerance = 1f; // +/- grams for "perfect" range
    [SerializeField] private float maxStorageCapacity = 100f; // Maximum coffee storage
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem coffeeParticles;
    [SerializeField] private AudioSource coffeeAddSound;
    [SerializeField] private AudioSource coffeeDispenseSound;
    [SerializeField] private Animator machineAnimator;
    
    // State variables
    private Portafilter currentPortafilter;
    private float storedCoffeeAmount = 0f; // Internal coffee storage
    private CoffeeQualityEvaluator qualityEvaluator;
    private bool isDispensing = false;
    
    private void Awake()
    {
        qualityEvaluator = new CoffeeQualityEvaluator(idealGramAmount, gramTolerance);
    }
    
    private void Start()
    {
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
            portafilterZone.AcceptPredicate = (item) => item is Portafilter && currentPortafilter == null;
        }
        
        if (groundCoffeeZone != null)
        {
            // Accept ground coffee regardless of portafilter presence
            groundCoffeeZone.AcceptPredicate = (item) => item is GroundCoffee;
        }
    }
    
    private void ConfigureGrammingButton()
    {
        if (grammingButton != null)
        {
            // Can interact if there's a portafilter and stored coffee
            grammingButton.CanInteract = () => currentPortafilter != null && storedCoffeeAmount > 0;
            grammingButton.OnHold = OnGrammingButtonHold;
            grammingButton.OnHoldRelease = OnGrammingButtonRelease;
        }
    }
    
    private void Update()
    {
        CheckPortafilterPresence();
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
                UpdateUI();
                UIManager.Instance.ShowNotification("Portafilter placed in gramming machine");
            }
        }
        // Detect when portafilter is removed
        else if (portafilterZone.transform.childCount == 0 && currentPortafilter != null)
        {
            currentPortafilter = null;
            UpdateUI();
        }
    }
    
    private void OnGrammingButtonHold(float duration)
    {
        if (currentPortafilter == null || storedCoffeeAmount <= 0)
            return;
        
        // Calculate amount to dispense
        float amountToDispense = grammingRate * Time.deltaTime;
        amountToDispense = Mathf.Min(amountToDispense, storedCoffeeAmount);
        
        if (amountToDispense > 0)
        {
            if (!isDispensing)
            {
                isDispensing = true;
                PlayDispenseEffects();
            }
            
            // Remove from storage
            storedCoffeeAmount -= amountToDispense;
            
            // Add to portafilter
            currentPortafilter.TryAddItem("ground_coffee", amountToDispense);
            
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
        isDispensing = false;
        StopDispenseEffects();
        
        // Provide feedback on current coffee quality
        if (currentPortafilter != null)
        {
            float coffeeAmount = currentPortafilter.GetItemAmount("ground_coffee");
            float quality = qualityEvaluator.EvaluateQuality(coffeeAmount);
            string qualityDesc = qualityEvaluator.GetQualityDescription(quality);
            
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
            
        // Check if adding would exceed capacity
        if (storedCoffeeAmount + amount > maxStorageCapacity)
        {
            float actualAmount = maxStorageCapacity - storedCoffeeAmount;
            storedCoffeeAmount = maxStorageCapacity;
            
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
        
        if (coffeeDispenseSound != null && !coffeeDispenseSound.isPlaying)
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
        // ...existing code remains unchanged...
        if (qualityIndicator == null) return;
        
        if (currentPortafilter != null)
        {
            float coffeeAmount = currentPortafilter.GetItemAmount("ground_coffee");
            float quality = qualityEvaluator.EvaluateQuality(coffeeAmount);
            qualityIndicator.color = qualityGradient.Evaluate(quality);
            
            float fillAmount = 0f;
            
            if (coffeeAmount > 0)
            {
                if (coffeeAmount <= idealGramAmount)
                {
                    fillAmount = coffeeAmount / idealGramAmount;
                }
                else
                {
                    float overFillRange = idealGramAmount * 1.5f - idealGramAmount;
                    float overAmount = coffeeAmount - idealGramAmount;
                    fillAmount = 1f - (overAmount / overFillRange) * 0.5f;
                }
            }
            
            qualityIndicator.fillAmount = fillAmount;
            qualityIndicator.gameObject.SetActive(true);
        }
        else
        {
            qualityIndicator.color = Color.white;
            qualityIndicator.fillAmount = storedCoffeeAmount / maxStorageCapacity;
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
            
            // Add to storage instead of directly to portafilter
            TryAddCoffee(coffeeAmount);
            
            // Visual feedback
            if (coffeeParticles != null)
            {
                coffeeParticles.Play();
                Invoke("StopParticles", 1.0f); // Stop particles after a delay
            }
            
            // Sound
            if (coffeeAddSound != null)
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
}