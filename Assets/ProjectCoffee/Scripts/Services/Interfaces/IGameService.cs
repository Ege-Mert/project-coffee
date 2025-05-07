namespace ProjectCoffee.Services.Interfaces
{
    /// <summary>
    /// Interface for game management service operations
    /// </summary>
    public interface IGameService
    {
        // Properties
        int Money { get; }
        float DayTimeRemaining { get; }
        int CurrentDay { get; }
        bool IsDayActive { get; }
        
        // Methods
        void StartDay();
        void EndDay();
        void AddMoney(int amount);
        bool TrySpendMoney(int amount);
    }
}