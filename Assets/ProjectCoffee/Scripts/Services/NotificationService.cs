using UnityEngine;
using ProjectCoffee.Services.Interfaces;

namespace ProjectCoffee.Services
{
    /// <summary>
    /// Service to handle user notifications via the UIManager
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly UIManager _uiManager;
        
        public NotificationService(UIManager uiManager)
        {
            _uiManager = uiManager;
        }
        
        /// <summary>
        /// Show a notification message to the user
        /// </summary>
        public void ShowNotification(string message)
        {
        if (_uiManager != null)
        {
        _uiManager.ShowNotification(message);
            return;
        }
        
        // Try to find UIManager in scene as fallback
        UIManager uiManager = UIManager.Instance;
        if (uiManager == null)
        {
            uiManager = GameObject.FindObjectOfType<UIManager>();
        }
        
        if (uiManager != null)
        {
            uiManager.ShowNotification(message);
            Debug.LogWarning("NotificationService had to find UIManager as fallback. Fix initialization order.");
        }
        else
        {
            Debug.LogError($"NotificationService: Cannot show notification '{message}' - UIManager not found in scene");
            // Fallback to console log so we at least see the notification
            Debug.Log($"NOTIFICATION: {message}");
        }
        }
        
        /// <summary>
        /// Show a tooltip at a specific position
        /// </summary>
        public void ShowTooltip(string message, Vector2 position)
        {
        if (_uiManager != null)
        {
        _uiManager.ShowTooltip(message, position);
            return;
        }
        
        // Try to find UIManager in scene as fallback
        UIManager uiManager = UIManager.Instance;
        if (uiManager == null)
        {
            uiManager = GameObject.FindObjectOfType<UIManager>();
        }
        
        if (uiManager != null)
        {
            uiManager.ShowTooltip(message, position);
            Debug.LogWarning("NotificationService had to find UIManager as fallback. Fix initialization order.");
        }
        else
        {
            Debug.LogError($"NotificationService: Cannot show tooltip '{message}' - UIManager not found in scene");
            // Fallback to console log so we at least see the notification
            Debug.Log($"TOOLTIP: {message}");
        }
        }
    }
}