# Drop Zone Setup Guide

## Overview
This guide explains how to properly configure the drop zones in Unity to fix the overlapping and interaction issues.

## Component Requirements

### For Espresso Machine

1. **Espresso Machine GameObject**
   - Must have `EspressoMachine` component

2. **Each Brewing Slot** (4 total slots)
   - **Portafilter Zone GameObject**:
     - Must have `EspressoMachineDropZone` component (NOT the base `DropZone`)
     - Set `Slot Index` to match the array index (0-3)
     - Set `Is Portafilter Zone` = true
     - Must have a Collider component set as Trigger
     - Must have an Image component for visual feedback
   
   - **Cup Zone GameObject**:
     - Must have `EspressoMachineDropZone` component (NOT the base `DropZone`)
     - Set `Slot Index` to match the array index (0-3)
     - Set `Is Portafilter Zone` = false
     - Must have a Collider component set as Trigger
     - Must have an Image component for visual feedback

3. **In EspressoMachine Component**:
   - Assign all 4 `BrewingSlotUI` entries
   - For each entry, assign the correct portafilter and cup zones

### For Gramming Machine

1. **Gramming Machine GameObject**
   - Must have `CoffeeGrammingMachine` component

2. **Portafilter Drop Zone**:
   - Remove the old `PortafilterDropZone` component if it exists
   - Add `GrammingPortafilterDropZone` component instead
   - Set `Parent Machine` reference to the CoffeeGrammingMachine
   - Must have a Collider component set as Trigger
   - Must have an Image component for visual feedback

## Step-by-Step Fix Instructions

### 1. Fix Espresso Machine Drop Zones

1. Select each portafilter drop zone in the espresso machine
2. Remove any existing `DropZone` or `PortafilterDropZone` components
3. Add `EspressoMachineDropZone` component
4. Configure:
   - `Slot Index`: Set to the correct slot (0-3)
   - `Is Portafilter Zone`: Check this box
   - `Is Active`: Should be checked
   - `Center Item In Zone`: Should be checked
   - `Preserve Item Size`: Should be checked

5. Repeat for cup zones but with `Is Portafilter Zone` unchecked

### 2. Fix Gramming Machine Drop Zone

1. Select the portafilter drop zone in the gramming machine
2. Remove the existing `PortafilterDropZone` component
3. Add `GrammingPortafilterDropZone` component
4. Configure:
   - `Parent Machine`: Drag the gramming machine GameObject here
   - `Is Active`: Should be checked
   - `Center Item In Zone`: Should be checked
   - `Preserve Item Size`: Should be checked

### 3. Verify References

In the EspressoMachine component:
1. Check that all `BrewingSlotUI` entries are properly assigned
2. Each entry should have:
   - `Portafilter Zone`: The GameObject with EspressoMachineDropZone (isPortafilterZone = true)
   - `Cup Zone`: The GameObject with EspressoMachineDropZone (isPortafilterZone = false)
   - `Active Indicator`: A visual indicator GameObject
   - `Progress Fill`: An Image component for showing brew progress

### 4. Common Issues and Solutions

**Issue**: Items still overlapping
- **Solution**: Make sure the drop zone has the new component with item tracker
- Check console for "Added DropZoneItemTracker" messages

**Issue**: Can't drop items on espresso machine
- **Solution**: Verify the EspressoMachineDropZone component is present and configured
- Check that `Slot Index` is set correctly (0-3)
- Ensure the Collider is set as a Trigger

**Issue**: Items stay transparent after brewing
- **Solution**: The DraggableStateManager should be automatically added
- Check console for state change messages

## Testing

1. **Test Overlapping Prevention**:
   - Try dropping two portafilters on the same zone
   - The second one should return to its original position

2. **Test Espresso Machine**:
   - Drop a portafilter in each slot
   - Drop a cup under each portafilter
   - Start brewing
   - Items should become semi-transparent during brewing
   - After brewing, items should be fully opaque and draggable again

3. **Test Gramming Machine**:
   - Drop a portafilter on the gramming machine
   - Try dropping another - it should be rejected

## Debug Messages

Enable debug logs to troubleshoot:
- "Item tracker rejected..." - Working correctly, preventing overlap
- "Initialized portafilter zone for slot X" - Zone properly initialized
- "Setting portafilter in slot X" - Item detection working
- "Updated slot X items to processing state" - State management working

## Final Checklist

- [ ] All espresso machine zones use `EspressoMachineDropZone`
- [ ] Gramming machine uses `GrammingPortafilterDropZone`
- [ ] No zones use the old `PortafilterDropZone` or base `DropZone`
- [ ] All slot indices are correctly set (0-3)
- [ ] All parent machine references are assigned
- [ ] Console shows initialization messages for all zones
- [ ] No overlapping items possible
- [ ] Items properly change state during brewing
