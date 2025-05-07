using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectCoffee.Core
{
    /// <summary>
    /// Handles initializing the game and ensuring required systems are present
    /// </summary>
    public class GameInitializer : MonoBehaviour
    {
        [SerializeField] private GameObject serviceManagerPrefab;
        [SerializeField] private GameObject gameManagerPrefab;
        [SerializeField] private GameObject uiManagerPrefab;
        
        private void Awake()
        {
            Debug.Log("GameInitializer: Starting initialization...");
            
            // Ensure we have a ServiceManager
            EnsureServiceManagerExists();
            
            // Ensure we have a GameManager
            EnsureGameManagerExists();
            
            // Ensure we have a UIManager
            EnsureUIManagerExists();
            
            // Subscribe to scene load events to ensure required managers exist in all scenes
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            Debug.Log("GameInitializer: Initialization complete");
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from scene load events
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"GameInitializer: Scene '{scene.name}' loaded, ensuring required managers exist");
            
            // Make sure required managers exist in the new scene
            EnsureServiceManagerExists();
            EnsureGameManagerExists();
            EnsureUIManagerExists();
        }
        
        private void EnsureServiceManagerExists()
        {
            if (ServiceManager.Instance == null && serviceManagerPrefab != null)
            {
                Debug.Log("GameInitializer: Creating ServiceManager");
                Instantiate(serviceManagerPrefab);
            }
        }
        
        private void EnsureGameManagerExists()
        {
            if (FindObjectOfType<GameManager>() == null && gameManagerPrefab != null)
            {
                Debug.Log("GameInitializer: Creating GameManager");
                Instantiate(gameManagerPrefab);
            }
        }
        
        private void EnsureUIManagerExists()
        {
            if (FindObjectOfType<UIManager>() == null && uiManagerPrefab != null)
            {
                Debug.Log("GameInitializer: Creating UIManager");
                Instantiate(uiManagerPrefab);
            }
        }
    }
}