using UnityEngine;

/// <summary>
/// Configuration for the espresso machine
/// </summary>
[CreateAssetMenu(fileName = "EspressoMachineConfig", menuName = "Coffee Game/Machine Configs/Espresso Machine Config")]
public class EspressoMachineConfig : MachineConfig
{
    [Header("Espresso Machine Specific Settings")]
    public float brewingTime = 5f;
    public int initialSlotCount = 1;
    public int maxSlotCount = 4;
    
    // Quality factors
    [Header("Quality Settings")]
    public float coffeeQualityWeight = 0.7f; // How much the coffee quality affects espresso quality
    public float temperatureQualityWeight = 0.3f; // How much temperature affects quality
    
    // Level-specific settings
    [Header("Level 0 Settings")]
    public float level0BrewTime = 5f;
    
    [Header("Level 1 Settings")]
    public float level1BrewTime = 3f; // Faster brewing
    
    [Header("Level 2 Settings")]
    public int level2ExtraSlots = 2; // Additional brewing slots
    public float level2BrewTime = 2f; // Even faster brewing
}
