using System;
using System.Collections.Generic;

namespace ProjectCoffee.Services.Interfaces
{
    /// <summary>
    /// Interface for espresso machine service
    /// </summary>
    public interface IEspressoMachineService : IMachineService
    {
        // Inner class definitions
        public interface IBrewingSlot
        {
            bool isActive { get; set; }
            float brewProgress { get; set; }
            bool hasPortafilter { get; set; }
            bool hasCup { get; set; }
            bool hasGroundCoffee { get; set; }
            float coffeeQuality { get; set; }
        }
        
        // Events
        event Action<int> OnSlotStateChanged;
        event Action<int, float> OnSlotProgressChanged;
        event Action<int> OnBrewingCompleted;
        
        // Methods
        void SetPortafilter(int slotIndex, bool present, bool hasGroundCoffee = false, float quality = 0f);
        void SetCup(int slotIndex, bool present);
        bool CanBrewSlot(int slotIndex);
        bool StartBrewingSlot(int slotIndex);
        void UpdateBrewing(float deltaTime);
        IBrewingSlot GetSlot(int slotIndex);
        bool CanBrewAnySlot();
        void BrewAllReadySlots();
    }
}