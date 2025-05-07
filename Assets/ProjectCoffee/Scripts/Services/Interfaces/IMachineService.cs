using System;

namespace ProjectCoffee.Services.Interfaces
{
    /// <summary>
    /// Interface for machine services to standardize interactions
    /// </summary>
    public interface IMachineService
    {
        // Properties
        MachineState CurrentState { get; }
        int UpgradeLevel { get; }
        float ProcessProgress { get; }
        
        // Events
        event Action<MachineState> OnStateChanged;
        event Action<int> OnUpgradeApplied;
        event Action<float> OnProgressChanged;
        event Action OnProcessCompleted;
        event Action<string> OnNotificationRequested;
        
        // Methods
        void SetUpgradeLevel(int level);
        bool CanProcess();
        void StartProcessing();
        void Reset();
    }
}