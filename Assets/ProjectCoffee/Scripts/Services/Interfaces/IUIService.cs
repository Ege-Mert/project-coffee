using UnityEngine;

namespace ProjectCoffee.Services.Interfaces
{
    /// <summary>
    /// Interface for UI service operations
    /// </summary>
    public interface IUIService
    {
        /// <summary>
        /// Show the end of day results screen
        /// </summary>
        void ShowEndOfDayScreen();
        
        /// <summary>
        /// Show the upgrade screen for purchasing machine upgrades
        /// </summary>
        void ShowUpgradeScreen();
        
        /// <summary>
        /// Show a notification to the user
        /// </summary>
        void ShowNotification(string message);
        
        /// <summary>
        /// Show a tooltip at a specific position
        /// </summary>
        void ShowTooltip(string message, Vector2 position);
        
        /// <summary>
        /// Update the money display
        /// </summary>
        void UpdateMoneyDisplay(int amount);
        
        /// <summary>
        /// Hide all UI screens
        /// </summary>
        void HideAllScreens();
    }
}