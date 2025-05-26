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
    [Tooltip("Description of Level 0 features")]
    public string level0Description = "Basic espresso machine with 2 brewing slots.";
    
    [Header("Level 1 Settings")]
    public float level1BrewTime = 3f; // Faster brewing
    [Tooltip("Description of Level 1 features")]
    public string level1Description = "Improved brewing time and better quality espresso.";
    
    [Header("Level 2 Settings")]
    public int level2ExtraSlots = 2; // Additional brewing slots
    public float level2BrewTime = 2f; // Even faster brewing
    [Tooltip("Description of Level 2 features")]
    public string level2Description = "Automatic brewing, 4 total slots, faster brewing time, and consistent quality.";
    public bool level2EnableAutoBrewing = true; // Enable automatic brewing
}
