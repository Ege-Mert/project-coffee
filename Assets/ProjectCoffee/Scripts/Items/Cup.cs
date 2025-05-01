using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cup for the final drink
/// </summary>
public class Cup : Container
{
    [SerializeField] private Image fillImage;
    [SerializeField] private Color emptyColor = Color.white;
    [SerializeField] private Color espressoColor = new Color(0.3f, 0.2f, 0.1f);
    [SerializeField] private Color milkColor = new Color(0.9f, 0.8f, 0.7f);
    [SerializeField] private float maxCapacity = 12f; // In oz
    [SerializeField] private AudioSource pourSound;
    
    private float currentFillAmount = 0f;
    
    protected override void Awake()
    {
        base.Awake();
        UpdateVisuals();
    }
    
    public override bool TryAddItem(string itemId, float amount = 1f)
    {
        // Check total capacity
        float newTotalAmount = currentFillAmount + amount;
        
        if (newTotalAmount > maxCapacity)
        {
            // Cup would overflow
            UIManager.Instance.ShowNotification("Cup cannot hold any more liquid!");
            return false;
        }
        
        bool result = base.TryAddItem(itemId, amount);
        
        if (result)
        {
            currentFillAmount += amount;
            UpdateVisuals();
            
            // Play pour sound
            if (pourSound != null)
            {
                pourSound.Play();
            }
        }
        
        return result;
    }
    
    public override bool TryRemoveItem(string itemId, float amount = 1f)
    {
        if (base.TryRemoveItem(itemId, amount))
        {
            currentFillAmount -= amount;
            UpdateVisuals();
            return true;
        }
        
        return false;
    }
    
    public override void Clear()
    {
        base.Clear();
        currentFillAmount = 0f;
        UpdateVisuals();
    }
    
    protected override void OnContentsChanged()
    {
        UpdateVisuals();
    }
    
    private void UpdateVisuals()
    {
        if (fillImage == null)
            return;
            
        // Update fill level visual
        float fillRatio = currentFillAmount / maxCapacity;
        fillImage.fillAmount = fillRatio;
        
        // Update color based on contents
        Color targetColor = emptyColor;
        
        // Mix colors based on ingredients
        if (contents.Count > 0)
        {
            // Start with coffee color if present
            if (ContainsItem("espresso"))
            {
                targetColor = espressoColor;
                
                // Add milk if present (lightens the color)
                if (ContainsItem("milk") || ContainsItem("steamed_milk"))
                {
                    float milkAmount = GetItemAmount("milk") + GetItemAmount("steamed_milk");
                    float milkRatio = Mathf.Clamp01(milkAmount / 8f);
                    targetColor = Color.Lerp(targetColor, milkColor, milkRatio);
                }
                
                // Add syrup color effects
                if (ContainsItem("chocolate_syrup"))
                {
                    targetColor = Color.Lerp(targetColor, new Color(0.2f, 0.1f, 0.05f), 0.3f);
                }
                else if (ContainsItem("caramel_syrup"))
                {
                    targetColor = Color.Lerp(targetColor, new Color(0.6f, 0.4f, 0.1f), 0.3f);
                }
                else if (ContainsItem("vanilla_syrup"))
                {
                    targetColor = Color.Lerp(targetColor, new Color(0.8f, 0.7f, 0.5f), 0.2f);
                }
                else if (ContainsItem("strawberry_syrup"))
                {
                    targetColor = Color.Lerp(targetColor, new Color(0.9f, 0.3f, 0.4f), 0.2f);
                }
            }
            else if (ContainsItem("milk") || ContainsItem("steamed_milk"))
            {
                // Just milk
                targetColor = milkColor;
            }
        }
        
        fillImage.color = targetColor;
    }
}
