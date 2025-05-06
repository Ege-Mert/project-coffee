using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ProjectCoffee.UI;

/// <summary>
/// UI manager for notifications and displays
/// </summary>
public class UIManager : MonoBehaviour
{
    private static UIManager _instance;
    public static UIManager Instance => _instance;
    
    [SerializeField] private Text moneyText;
    [SerializeField] private Text timeText;
    [SerializeField] private Text dayText;
    [SerializeField] private GameObject endOfDayPanel;
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private Text notificationText;
    [SerializeField] private UpgradeUI upgradeUI;
    [SerializeField] private Button upgradeButton;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
    }
    
    private void Start()
    {
        // Subscribe to events
        GameManager.Instance.OnMoneyChanged += UpdateMoneyDisplay;
        GameManager.Instance.OnDayStarted += OnDayStarted;
        GameManager.Instance.OnDayEnded += OnDayEnded;
        
        // Setup upgrade button
        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(ShowUpgradeScreen);
        }
        
        UpdateMoneyDisplay(GameManager.Instance.Money);
        UpdateDayDisplay(GameManager.Instance.CurrentDay);
        
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
        
        if (endOfDayPanel != null)
        {
            endOfDayPanel.SetActive(false);
        }
    }
    
    private void Update()
    {
        if (GameManager.Instance.IsDayActive)
        {
            UpdateTimeDisplay(GameManager.Instance.DayTimeRemaining);
        }
    }
    
    private void UpdateMoneyDisplay(int amount)
    {
        if (moneyText != null)
        {
            moneyText.text = $"${amount}";
        }
    }
    
    private void UpdateTimeDisplay(float timeRemaining)
    {
        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            timeText.text = $"{minutes:00}:{seconds:00}";
        }
    }
    
    private void UpdateDayDisplay(int day)
    {
        if (dayText != null)
        {
            dayText.text = $"Day {day}";
        }
    }
    
    private void OnDayStarted(int day)
    {
        UpdateDayDisplay(day);
    }
    
    private void OnDayEnded(int day)
    {
        // Day has ended
    }
    
    public void ShowEndOfDayScreen()
    {
        if (endOfDayPanel != null)
        {
            endOfDayPanel.SetActive(true);
        }
    }
    
    public void CloseEndOfDayScreen()
    {
        if (endOfDayPanel != null)
        {
            endOfDayPanel.SetActive(false);
        }
    }
    
    public void ShowNotification(string message, float duration = 3f)
    {
        if (notificationPanel != null && notificationText != null)
        {
            notificationText.text = message;
            notificationPanel.SetActive(true);
            
            // Auto-hide after delay
            StartCoroutine(HideNotificationAfterDelay(duration));
        }
    }
    
    private IEnumerator HideNotificationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
    }
    
    // Start the day (connect to a start button)
    public void OnStartDayButtonClicked()
    {
        GameManager.Instance.StartDay();
        
        // Hide end of day panel if showing
        if (endOfDayPanel != null)
        {
            endOfDayPanel.SetActive(false);
        }
    }
    
    public void ShowUpgradeScreen()
    {
        if (upgradeUI != null)
        {
            upgradeUI.Show();
        }
    }
    
    public void CloseUpgradeScreen()
    {
        if (upgradeUI != null)
        {
            upgradeUI.Hide();
        }
    }
}
