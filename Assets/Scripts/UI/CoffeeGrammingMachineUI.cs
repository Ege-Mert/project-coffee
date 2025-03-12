using TMPro;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Coffee gramming machine for tamping coffee
/// </summary>
public class CoffeeGrammingMachineUI : MonoBehaviour
{
    [SerializeField] private DropZoneUI portafilterZone;
    [SerializeField] private DropZoneUI groundCoffeeZone;
    [SerializeField] private HoldableUI grammingButton;
    [SerializeField] private TMP_Text gramDisplayText;
    [SerializeField] private Image qualityIndicator;
    [SerializeField] private Gradient qualityGradient;
    [SerializeField] private float grammingRate = 6f; // Grams per second
    [SerializeField] private float idealGramAmount = 18f;
    [SerializeField] private float gramTolerance = 1f; // +/- grams for "perfect" range
    [SerializeField] private ParticleSystem coffeeParticles;
    [SerializeField] private AudioSource coffeeAddSound;
    [SerializeField] private AudioSource tampingSound;
    [SerializeField] private Animator machineAnimator;
    
    private Portafilter currentPortafilter;
    private float currentGramming = 0f;
    
    private void Start()
    {
        if (portafilterZone != null)
        {
            // Configure portafilter drop zone
            portafilterZone.AcceptPredicate = (item) => item is Portafilter && currentPortafilter == null;
        }
        
        if (groundCoffeeZone != null)
        {
            // Configure ground coffee drop zone
            groundCoffeeZone.AcceptPredicate = (item) => item is GroundCoffeeUI && currentPortafilter == null;
        }
        
        if (grammingButton != null)
        {
            // Configure gramming button
            grammingButton.CanInteract = () => currentPortafilter != null && currentGramming > 0;
            grammingButton.OnHold = OnGrammingHold;
            grammingButton.OnHoldRelease = OnGrammingRelease;
        }
        
        if (coffeeParticles != null)
        {
            coffeeParticles.Stop();
        }
        
        UpdateGramDisplay();
        UpdateQualityIndicator();
    }
    
    private void Update()
    {
        // Check for portafilter in drop zone
        if (portafilterZone != null && portafilterZone.transform.childCount > 0 && currentPortafilter == null)
        {
            Portafilter portafilter = portafilterZone.transform.GetChild(0).GetComponent<Portafilter>();
            if (portafilter != null)
            {
                currentPortafilter = portafilter;
                currentGramming = portafilter.GetItemAmount("ground_coffee");
                UpdateGramDisplay();
                UpdateQualityIndicator();
            }
        }
        else if (portafilterZone != null && portafilterZone.transform.childCount == 0 && currentPortafilter != null)
        {
            currentPortafilter = null;
            currentGramming = 0f;
            UpdateGramDisplay();
            UpdateQualityIndicator();
        }
        
        // Check for ground coffee being dropped
        if (groundCoffeeZone != null && groundCoffeeZone.transform.childCount > 0)
        {
            GroundCoffeeUI groundCoffee = groundCoffeeZone.transform.GetChild(0).GetComponent<GroundCoffeeUI>();
            if (groundCoffee != null && currentPortafilter != null)
            {
                float coffeeAmount = groundCoffee.GetAmount();
                currentPortafilter.TryAddItem("ground_coffee", coffeeAmount);
                
                currentGramming = currentPortafilter.GetItemAmount("ground_coffee");
                UpdateGramDisplay();
                UpdateQualityIndicator();
                
                // Visual feedback
                if (coffeeParticles != null)
                {
                    coffeeParticles.Play();
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
                
                // Show notification
                UIManager.Instance.ShowNotification($"Added {coffeeAmount}g of ground coffee");
                
                // Destroy the ground coffee object
                Destroy(groundCoffee.gameObject);
            }
        }
    }
    
    private void OnGrammingHold(float duration)
    {
        if (currentPortafilter == null || currentGramming <= 0)
            return;
        
        // Visual feedback
        if (coffeeParticles != null && !coffeeParticles.isPlaying)
        {
            coffeeParticles.Play();
        }
        
        // Sound
        if (tampingSound != null && !tampingSound.isPlaying)
        {
            tampingSound.Play();
        }
        
        // Animation
        if (machineAnimator != null)
        {
            machineAnimator.SetBool("Tamping", true);
        }
        
        // Adjust the coffee amount (tamp it down - reduce amount)
        float tamperStrength = Mathf.Lerp(0.1f, 0.5f, Mathf.Clamp01(duration / 3f));
        float gramReduction = grammingRate * Time.deltaTime * tamperStrength;
        
        // Don't let it go below a minimum threshold
        if (currentGramming - gramReduction >= 1f)
        {
            currentGramming -= gramReduction;
            currentPortafilter.TryRemoveItem("ground_coffee", gramReduction);
            
            // Update the display with current weight
            UpdateGramDisplay();
            UpdateQualityIndicator();
        }
    }
    
    private void OnGrammingRelease(float heldDuration)
    {
        if (coffeeParticles != null)
        {
            coffeeParticles.Stop();
        }
        
        if (tampingSound != null && tampingSound.isPlaying)
        {
            tampingSound.Stop();
        }
        
        if (machineAnimator != null)
        {
            machineAnimator.SetBool("Tamping", false);
        }
        
        // Finalize the gramming process
        if (currentPortafilter != null)
        {
            // Provide feedback based on coffee amount
            float quality = GetQualityFactor();
            
            if (quality > 0.9f)
            {
                UIManager.Instance.ShowNotification("Perfect tamping!");
            }
            else if (quality > 0.7f)
            {
                UIManager.Instance.ShowNotification("Good tamping");
            }
            else if (quality > 0.5f)
            {
                UIManager.Instance.ShowNotification("Acceptable tamping");
            }
            else
            {
                UIManager.Instance.ShowNotification("Poor tamping");
            }
        }
    }
    
    private float GetQualityFactor()
    {
        float deviation = Mathf.Abs(currentGramming - idealGramAmount);
        
        if (deviation <= gramTolerance)
        {
            return 1f; // Perfect
        }
        
        float maxDeviation = idealGramAmount * 0.5f; // 50% off is worst case
        return 1f - Mathf.Clamp01(deviation / maxDeviation);
    }
    
    private void UpdateGramDisplay()
    {
        if (gramDisplayText != null)
        {
            gramDisplayText.text = $"{currentGramming:F1}g";
        }
    }
    
    private void UpdateQualityIndicator()
    {
        if (qualityIndicator != null)
        {
            float quality = GetQualityFactor();
            qualityIndicator.color = qualityGradient.Evaluate(quality);
            
            // Also show how close to ideal weight
            float fillAmount = 0f;
            
            if (currentGramming > 0)
            {
                // Calculate how close to ideal
                if (currentGramming <= idealGramAmount)
                {
                    fillAmount = currentGramming / idealGramAmount;
                }
                else
                {
                    // Over ideal
                    float overFillRange = idealGramAmount * 1.5f - idealGramAmount;
                    float overAmount = currentGramming - idealGramAmount;
                    fillAmount = 1f - (overAmount / overFillRange) * 0.5f; // Start decreasing
                }
            }
            
            qualityIndicator.fillAmount = fillAmount;
        }
    }
    
    // Method to manually connect to portafilterZone's OnDrop event in inspector
    public void OnPortafilterDropped(DraggableUI item)
    {
        if (item is Portafilter portafilter)
        {
            currentPortafilter = portafilter;
            currentGramming = portafilter.GetItemAmount("ground_coffee");
            UpdateGramDisplay();
            UpdateQualityIndicator();
            
            UIManager.Instance.ShowNotification("Portafilter placed in gramming machine");
        }
    }
    
    // Method to manually connect to portafilterZone's OnItemRemoved event in inspector
    public void OnPortafilterRemoved(DraggableUI item)
    {
        if (item is Portafilter)
        {
            currentPortafilter = null;
            currentGramming = 0f;
            UpdateGramDisplay();
            UpdateQualityIndicator();
        }
    }
    
    // Method to manually connect to groundCoffeeZone's OnDrop event in inspector
    public void OnGroundCoffeeDropped(DraggableUI item)
    {
        if (item is GroundCoffeeUI groundCoffee && currentPortafilter != null)
        {
            float coffeeAmount = groundCoffee.GetAmount();
            currentPortafilter.TryAddItem("ground_coffee", coffeeAmount);
            
            currentGramming = currentPortafilter.GetItemAmount("ground_coffee");
            UpdateGramDisplay();
            UpdateQualityIndicator();
            
            // Visual feedback
            if (coffeeParticles != null)
            {
                coffeeParticles.Play();
            }
            
            // Show notification
            UIManager.Instance.ShowNotification($"Added {coffeeAmount}g of ground coffee");
            
            // Destroy the ground coffee object
            Destroy(groundCoffee.gameObject);
        }
    }
}