using UnityEngine;

namespace ProjectCoffee.Services.Interfaces
{
    /// <summary>
    /// Interface for notification service to display messages to the user
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Show a notification message to the user
        /// </summary>
        void ShowNotification(string message);
        
        /// <summary>
        /// Show a tooltip at a specific position
        /// </summary>
        void ShowTooltip(string message, Vector2 position);
    }
}