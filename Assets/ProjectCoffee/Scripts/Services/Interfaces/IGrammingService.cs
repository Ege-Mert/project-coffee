using System;

namespace ProjectCoffee.Services.Interfaces
{
    /// <summary>
    /// Interface for coffee gramming service
    /// </summary>
    public interface IGrammingService : IMachineService
    {
        // Properties
        float StoredCoffeeAmount { get; }
        bool HasPortafilter { get; }
        float PortafilterCoffeeAmount { get; }
        float MaxStorageCapacity { get; }
        
        // Events
        event Action<float> OnCoffeeAmountChanged;
        event Action<float> OnPortafilterFillChanged;
        event Action<CoffeeQualityEvaluator.QualityLevel> OnQualityEvaluated;
        
        // Methods
        bool AddCoffee(float amount);
        void SetPortafilterPresent(bool present);
        void OnDispensingHold(float deltaTime);
        void OnDispensingRelease();
        void ClearPortafilter();
        void CheckAutoOperation();
    }
}