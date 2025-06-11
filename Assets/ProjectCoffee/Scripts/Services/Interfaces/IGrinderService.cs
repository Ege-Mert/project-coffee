using System;

namespace ProjectCoffee.Services.Interfaces
{
    /// <summary>
    /// Interface for coffee grinder service
    /// </summary>
    public interface IGrinderService : IMachineService
    {
        // Grinder-specific properties
        int CurrentBeanFills { get; }
        int MaxBeanFills { get; }
        bool HasBeans { get; }
        bool CanAddBeans { get; }
        bool HasExistingGroundCoffee { get; }
        GroundCoffee.GrindSize CurrentCoffeeSize { get; }
        
        // Grinder-specific events
        event Action<int> OnBeanCountChanged;
        event Action<GroundCoffee.GrindSize> OnCoffeeOutputReady;
        event Action<int> OnSpinCompleted;
        event Action<GroundCoffee.GrindSize> OnCoffeeSizeUpgraded;
        
        // Grinder-specific methods
        bool AddBeans(int amount);
        void OnHandleSpinCompleted();
        void OnButtonPressed();
        void ProcessUpdate(float deltaTime);
        void OnGroundCoffeeRemoved();
        void CheckAutoProcess();
        void StopContinuousProcessing();
    }
}