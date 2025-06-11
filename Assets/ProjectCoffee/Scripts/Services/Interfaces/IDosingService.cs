using System;
using ProjectCoffee.Machines.Dosing.Logic;

namespace ProjectCoffee.Services.Interfaces
{
    /// <summary>
    /// Interface for coffee dosing service
    /// </summary>
    public interface IDosingService : IMachineService
    {
        // Properties
        float StoredCoffeeAmount { get; }
        bool HasPortafilter { get; }
        float PortafilterCoffeeAmount { get; }
        float MaxStorageCapacity { get; }
        
        // Events
        event Action<float> OnCoffeeAmountChanged;
        event Action<float> OnPortafilterFillChanged;
        event Action<QualityResult> OnQualityEvaluated;
        event Action OnAutoDoseStarted;
        event Action OnAutoDoseCompleted;
        event Action<bool> OnPortafilterPresenceChanged;
        
        // Core Methods
        bool AddCoffee(float amount);
        void SetPortafilterPresent(bool present);
        void OnDispensingHold(float deltaTime);
        void OnDispensingRelease();
        void ClearPortafilter();
        
        // Auto-dosing
        bool ShouldAutoDose();
        DosingCalculation PerformAutoDose();
        
        // Processing
        bool StartButtonProcess();
        void CompleteButtonProcess();
        float GetProcessingTime();
        InteractionType GetInteractionType();
        
        // State Management
        void ForceStateTransition(MachineState newState);
        string GetStateInfo();
    }
}
