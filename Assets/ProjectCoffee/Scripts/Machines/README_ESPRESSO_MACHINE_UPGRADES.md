# Espresso Machine Upgrade Implementation

This guide documents the implementation of the espresso machine upgrade system, which adds new functionality as the player progresses through the upgrade levels.

## Overview

The espresso machine has three upgrade levels:

1. **Level 0 (Basic)**: 
   - Manual brewing via button press
   - 2 brewing slots
   - 5-second brewing time
   - Quality dependent on input coffee beans

2. **Level 1 (Improved)**:
   - Manual brewing via button press
   - 2 brewing slots
   - 3-second brewing time (40% faster)
   - Improved espresso quality (+10% quality)

3. **Level 2 (Professional)**:
   - Automatic brewing when portafilter and cup are detected
   - 4 brewing slots (2 additional slots)
   - 2-second brewing time (60% faster than basic)
   - Guaranteed good quality (minimum 70% quality)
   - Further quality improvements (+20% quality)

## Setup Instructions

1. **Configure the Inspector**

   Ensure the following fields are set in the EspressoMachine component:
   
   - `brewingSlotUIs`: List of all available slots (should include the additional slots for level 2)
   - `brewButton`: Reference to the button for manual brewing
   - `autoBrewIndicator`: GameObject that indicates auto-brewing is active (level 2)
   - `manualBrewIndicator`: GameObject that indicates manual brewing is required (levels 0-1)

2. **UI Layout**

   - Arrange brewing slots in a logical layout
   - Initially, only the first 2 slots should be active
   - The additional slots (for level 2) should be positioned but inactive
   - Brew button should be clearly visible
   - Auto-brew indicator should be positioned but initially inactive

3. **Configuration Asset**

   Make sure the EspressoMachineConfig asset has appropriate values:
   
   - `level0BrewTime`: 5.0 seconds
   - `level1BrewTime`: 3.0 seconds
   - `level2BrewTime`: 2.0 seconds
   - `level2ExtraSlots`: 2
   - `level2EnableAutoBrewing`: true

## Upgrade Effects

Each upgrade level adds specific enhancements:

### Level 0 to Level 1
- Brewing time decreases from 5 to 3 seconds
- Espresso quality gets a +10% bonus
- UI remains mostly the same

### Level 1 to Level 2
- Two additional brewing slots become available
- Brewing time decreases to 2 seconds
- Auto-brewing activates, starting the brewing process automatically when:
  - A portafilter with ground coffee is placed in a slot
  - A cup is placed in the cup zone for that slot
- Auto-brew indicator appears
- Minimum quality floor of 70% ensures consistently good espresso
- Quality gets an additional +10% bonus (total +20%)

## Testing Checklist

- [ ] Level 0: Verify brewing time is 5 seconds
- [ ] Level 0: Check that quality matches input coffee beans
- [ ] Level 1: Verify brewing time is 3 seconds
- [ ] Level 1: Check that quality is improved over level 0
- [ ] Level 2: Verify all 4 slots are available
- [ ] Level 2: Confirm brewing time is 2 seconds
- [ ] Level 2: Test auto-brewing functionality
- [ ] Level 2: Verify minimum quality floor is applied
- [ ] All Levels: Visual indicators update correctly

## Troubleshooting

- **Auto-brewing not working**: Check if `upgradeLevel` in EspressoMachine is being set correctly
- **Additional slots not appearing**: Verify parent GameObject is being activated in `EnableAdditionalSlots`
- **Incorrect brewing times**: Check that `UpdateBrewTimeFromConfig` is being called during upgrades
- **Quality not improving**: Ensure `espressoQualityLevelBonus` is applied in `CompleteBrewing`

---

Implementation completed by [Your Name] on [Date]