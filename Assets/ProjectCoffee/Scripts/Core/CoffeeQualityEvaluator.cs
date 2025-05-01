using UnityEngine;

/// <summary>
/// Helper class for evaluating coffee quality based on various factors
/// </summary>
public class CoffeeQualityEvaluator
{
    private float idealGramAmount;
    private float gramTolerance;
    
    public CoffeeQualityEvaluator(float idealGramAmount, float gramTolerance)
    {
        this.idealGramAmount = idealGramAmount;
        this.gramTolerance = gramTolerance;
    }
    
    /// <summary>
    /// Evaluates the quality based on how close the amount is to the ideal amount
    /// </summary>
    /// <param name="actualAmount">The actual coffee amount in grams</param>
    /// <returns>Quality factor between 0 and 1</returns>
    public float EvaluateQuality(float actualAmount)
    {
        if (actualAmount <= 0)
            return 0f;
            
        // Calculate deviation from ideal
        float deviation = Mathf.Abs(actualAmount - idealGramAmount);
        
        // Perfect quality if within tolerance
        if (deviation <= gramTolerance)
            return 1f;
            
        // Linear falloff outside tolerance range, with a max deviation of 50% of ideal amount
        float maxDeviation = idealGramAmount * 0.5f;
        float qualityFactor = 1f - Mathf.Clamp01((deviation - gramTolerance) / (maxDeviation - gramTolerance));
        
        return qualityFactor;
    }
    
    /// <summary>
    /// Get a text description of the quality
    /// </summary>
    /// <param name="qualityFactor">Quality factor between 0 and 1</param>
    /// <returns>Text description of quality</returns>
    public string GetQualityDescription(float qualityFactor)
    {
        if (qualityFactor >= 0.95f)
            return "Perfect";
        else if (qualityFactor >= 0.8f)
            return "Excellent";
        else if (qualityFactor >= 0.6f)
            return "Good";
        else if (qualityFactor >= 0.4f)
            return "Acceptable";
        else if (qualityFactor >= 0.2f)
            return "Poor";
        else
            return "Terrible";
    }
}