using UnityEngine;

/// <summary>
/// Evaluates the quality of coffee based on its gram weight
/// </summary>
public class CoffeeQualityEvaluator
{
    private float idealWeight;
    private float tolerance;
    
    public CoffeeQualityEvaluator(float idealWeight, float tolerance)
    {
        this.idealWeight = idealWeight;
        this.tolerance = tolerance;
    }
    
    /// <summary>
    /// Evaluate the quality of coffee based on its weight
    /// </summary>
    /// <param name="actualWeight">Actual weight in grams</param>
    /// <returns>Quality factor from 0.0 (worst) to 1.0 (perfect)</returns>
    public float EvaluateQuality(float actualWeight)
    {
        if (actualWeight <= 0)
            return 0f;
            
        float deviation = Mathf.Abs(actualWeight - idealWeight);
        
        if (deviation <= tolerance)
            return 1f; // Perfect
            
        float maxDeviation = idealWeight * 0.5f; // 50% off is worst case
        return 1f - Mathf.Clamp01(deviation / maxDeviation);
    }
    
    /// <summary>
    /// Get a descriptive text of the coffee quality
    /// </summary>
    /// <param name="quality">Quality factor from 0.0 to 1.0</param>
    /// <returns>Text description of quality level</returns>
    public string GetQualityDescription(float quality)
    {
        if (quality > 0.9f)
            return "Perfect";
        else if (quality > 0.7f)
            return "Good";
        else if (quality > 0.5f)
            return "Acceptable";
        else if (quality > 0.3f)
            return "Poor";
        else
            return "Terrible";
    }
}