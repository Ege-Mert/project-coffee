using System;
using UnityEngine;

/// <summary>
/// Base ScriptableObject for machine configurations
/// </summary>
public abstract class MachineConfig : ScriptableObject
{
    [Header("Base Settings")]
    public string machineId;
    public string displayName;
    public int maxUpgradeLevel = 2;
    public float baseProcessTime = 3f;
    
    [Header("Upgrade Settings")]
    public UpgradeLevelData[] upgradeLevels;
}

/// <summary>
/// Data for a specific machine upgrade level
/// </summary>
[Serializable]
public class UpgradeLevelData
{
    public string upgradeName;
    public string description;
    public int upgradeCost;
    public float processTimeMultiplier = 1f;
    public InteractionType interactionType;
    public Sprite upgradeIcon;
}

/// <summary>
/// Types of interactions available for machines
/// </summary>
public enum InteractionType
{
    ManualLever,
    ButtonPress,
    ButtonHold,
    DragAndDrop,
    AutoProcess
}
