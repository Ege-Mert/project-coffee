using UnityEngine;

/// <summary>
/// Configuration for the coffee dosing machine
/// </summary>
[CreateAssetMenu(fileName = "DosingMachineConfig", menuName = "Coffee Game/Machine Configs/Dosing Machine Config")]
public class DosingMachineConfig : MachineConfig
{
    [Header("Dosing Machine Specific Settings")]
    public float grammingRate = 2f; // Grams per second when dispensing
    public float idealGramAmount = 18f; 
    public float gramTolerance = 1f; // +/- grams for "perfect" range
    public float maxStorageCapacity = 100f; // Maximum coffee storage
    
    // Quality calculation thresholds
    [Header("Quality Settings")]
    public float poorQualityThreshold = 0.5f;
    public float goodQualityThreshold = 0.8f;
    
    // Level-specific settings
    [Header("Level 0 Settings")]
    public float level0HoldFactor = 1f;
    
    [Header("Level 1 Settings")]
    public float level1AutoDoseTime = 2f;
    public float level1DoseAmount = 18f;
    
    [Header("Level 2 Settings")]
    public float level2AutoDetectionInterval = 0.5f;
}
