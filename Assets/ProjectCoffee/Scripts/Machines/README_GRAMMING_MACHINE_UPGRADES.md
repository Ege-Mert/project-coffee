# Gramming Machine Upgrade Implementation

This guide explains how to set up and implement the upgrade system for the Coffee Gramming Machine, building on the pattern established with the Coffee Grinder.

## Overview

The Gramming Machine has three upgrade levels:

1. **Level 0 (Manual)**: Hold button to dispense coffee - requires precise control from player
2. **Level 1 (Semi-Auto)**: Single button click dispenses exactly 18g of coffee
3. **Level 2 (Automatic)**: Automatically dispenses when portafilter is detected

## Setup Instructions

1. **Configure the Inspector**

   Open the CoffeeGrammingMachine in the Inspector and ensure all required components are assigned:

   - `grammingButton` (Holdable): The button players hold for Level 0 manual dosing
   - `autoDoseButton` (Button): The single-click button for Level 1 semi-auto dosing
   - `autoDosingIndicator` (GameObject): Visual indicator that appears in Level 2 automatic mode

2. **UI Layout**

   Position all UI elements correctly in your scene:

   - The `grammingButton` should be prominently visible at Level 0
   - The `autoDoseButton` should be positioned in the same area but initially inactive
   - The `autoDosingIndicator` should indicate the area is now automatic (e.g., "AUTO" text or icon)

3. **Test Each Level**

   Use the UpgradeService to test each level individually:

   ```csharp
   // Get the gramming service
   var grammingService = ServiceLocator.Instance.GetService<IGrammingService>();
   
   // Set the upgrade level (0, 1, or 2)
   grammingService.SetUpgradeLevel(level);
   ```

## Level-Specific Behaviors

### Level 0: Manual Hold

- Players must hold the `grammingButton` to control dispensing amount
- The longer they hold, the more coffee is dispensed
- The perfect amount (18g) requires precise timing

### Level 1: Semi-Auto Single Press

- The `autoDoseButton` replaces the hold button
- One click automatically dispenses the perfect 18g amount
- Simulates visual feedback during a brief processing period
- Always gives perfect quality results

### Level 2: Fully Automatic

- No buttons required - everything is automatic
- When a portafilter is placed, coffee is automatically dispensed
- Visual feedback shows the machine working automatically
- Always gives perfect quality results
- The `autoDosingIndicator` shows the machine is in automatic mode

## Testing Checklist

- [ ] Level 0: Verify hold behavior works properly
- [ ] Level 0: Check that quality evaluation is correct
- [ ] Level 1: Verify button appears and manual hold disappears
- [ ] Level 1: Confirm single press gives perfect 18g
- [ ] Level 2: Verify buttons disappear and indicator appears
- [ ] Level 2: Confirm auto-dosing when portafilter is placed
- [ ] All Levels: Quality feedback is shown correctly

## Known Issues and Troubleshooting

- If the portafilter isn't being detected, check the `portafilterZone` accept predicate
- If auto-dosing doesn't trigger, ensure the machine state is transitioning correctly
- For quality assessment issues, verify the `qualityEvaluator` parameters

---

With these elements in place, your Gramming Machine should fully support all three upgrade levels similar to the Grinder implementation.
