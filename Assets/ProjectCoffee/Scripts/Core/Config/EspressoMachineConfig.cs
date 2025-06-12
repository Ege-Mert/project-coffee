using UnityEngine;

/// <summary>
/// Configuration for the espresso machine
/// </summary>
[CreateAssetMenu(fileName = "EspressoMachineConfig", menuName = "Coffee Game/Machine Configs/Espresso Machine Config")]
public class EspressoMachineConfig : MachineConfig
{
    [Header("Espresso Machine Specific Settings")]
    public float brewingTime = 5f;
    public int initialSlotCount = 2; // Start with 2 slots visible
    public int maxSlotCount = 4; // Total 4 slots when fully upgraded
    
    // Quality factors
    [Header("Quality Settings")]
    public float coffeeQualityWeight = 0.7f; // How much the coffee quality affects espresso quality
    public float temperatureQualityWeight = 0.3f; // How much temperature affects quality
    
    // Level-specific settings
    [Header("Level 0 Settings")]
    public float level0BrewTime = 5f; // Base brewing time
    [Tooltip("Description of Level 0 features")]
    public string level0Description = "Basic espresso machine with 2 brewing slots and manual brewing.";
    
    [Header("Level 1 Settings")]
    public float level1BrewTime = 2.5f; // Half the brew time (5f / 2 = 2.5f)
    [Tooltip("Description of Level 1 features")]
    public string level1Description = "Faster brewing time - brews twice as fast!";
    
    [Header("Level 2 Settings")]
    public int level2ExtraSlots = 2; // Additional brewing slots (2 + 2 = 4 total)
    public float level2BrewTime = 2.5f; // Keep the fast time from level 1
    [Tooltip("Description of Level 2 features")]
    public string level2Description = "Automatic brewing and 4 total slots - maximum efficiency!";
    public bool level2EnableAutoBrewing = true; // Enable automatic brewing
}
