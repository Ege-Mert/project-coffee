using UnityEngine;

/// <summary>
/// Configuration for the coffee grinder machine
/// </summary>
[CreateAssetMenu(fileName = "GrinderConfig", menuName = "Coffee Game/Machine Configs/Grinder Config")]
public class GrinderConfig : MachineConfig
{
    [Header("Grinder Specific Settings")]
    public int maxBeanFills = 3;
    public int spinsPerStage = 1;
    public float[] groundCoffeeSizes = { 6f, 12f, 18f }; // Small, Medium, Large in grams
    
    // Level-specific settings
    [Header("Level 0 Settings")]
    public int level0SpinsRequired = 3;
    
    [Header("Level 1 Settings")]
    public float level1GrindTime = 3f;
    
    [Header("Level 2 Settings")]
    public float level2GrindTime = 1.5f; // Shorter grinding time for automatic level
}
