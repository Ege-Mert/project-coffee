using UnityEngine;
using UnityEngine.UI;
using ProjectCoffee.Items;

/// <summary>
/// Portafilter for holding ground coffee
/// </summary>
public class Portafilter : Container
{
    [SerializeField] private Image coffeeImage;
    [SerializeField] private float idealCoffeeGrams = 18f;
    [SerializeField] private float maxCoffeeGrams = 24f;
    [SerializeField] private Gradient coffeeColorGradient;
    
    public bool HasGroundCoffee => ContainsItem("ground_coffee", 0.1f);
    
    protected override void Awake()
    {
        base.Awake();
        
        // Ensure we have a DraggableItemInitializer component
        if (GetComponent<DraggableItemInitializer>() == null)
        {
            gameObject.AddComponent<DraggableItemInitializer>();
        }
    }
    
    protected override void OnContentsChanged()
    {
        UpdateVisuals();
    }
    
    private void UpdateVisuals()
    {
        if (coffeeImage == null)
            return;
            
        float coffeeAmount = GetItemAmount("ground_coffee");
        bool hasCoffee = coffeeAmount > 0;
        
        coffeeImage.gameObject.SetActive(hasCoffee);
        
        if (hasCoffee)
        {
            // Scale the visual based on amount
            float visualScale = Mathf.Clamp01(coffeeAmount / maxCoffeeGrams);
            coffeeImage.transform.localScale = new Vector3(1f, visualScale, 1f);
            
            // Color based on amount (more = darker)
            float colorGradientPos = Mathf.Clamp01(coffeeAmount / maxCoffeeGrams);
            coffeeImage.color = coffeeColorGradient.Evaluate(colorGradientPos);
        }
    }
    
    public float GetCoffeeQualityFactor()
    {
        float coffeeAmount = GetItemAmount("ground_coffee");
        
        if (coffeeAmount <= 0)
        {
            return 0f; // No coffee at all
        }
        
        // Calculate quality factor (1.0 = perfect)
        float deviation = Mathf.Abs(coffeeAmount - idealCoffeeGrams);
        float maxDeviation = idealCoffeeGrams * 0.5f; // 50% off is worst case
        
        float qualityFactor = 1f - Mathf.Clamp01(deviation / maxDeviation);
        return qualityFactor;
    }
}