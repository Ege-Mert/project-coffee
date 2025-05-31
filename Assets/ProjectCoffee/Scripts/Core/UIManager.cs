using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ProjectCoffee.UI;
using ProjectCoffee.Core;
using ProjectCoffee.Services.Interfaces;
using TMPro;

public class UIManager : MonoBehaviour, IUIService
{
    private static UIManager _instance;
    public static UIManager Instance => _instance;
    
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private GameObject endOfDayPanel;
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TMP_Text notificationText;
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
        EventBus.OnMoneyChanged += UpdateMoneyDisplay;
        EventBus.OnDayStarted += OnDayStarted;
        EventBus.OnDayEnded += OnDayEnded;
        
        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(ShowUpgradeScreen);
        
        UpdateMoneyDisplay(Services.Game?.Money ?? 0);
        UpdateDayDisplay(Services.Game?.CurrentDay ?? 1);
        
        if (notificationPanel != null)
            notificationPanel.SetActive(false);
        
        if (endOfDayPanel != null)
            endOfDayPanel.SetActive(false);
    }
    
    private void Update()
    {
        if (Services.Game != null && Services.Game.IsDayActive)
            UpdateTimeDisplay(Services.Game.DayTimeRemaining);
    }
    
    public void UpdateMoneyDisplay(int amount)
    {
        if (moneyText != null)
            moneyText.text = $"${amount}";
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
            dayText.text = $"Day {day}";
    }
    
    private void OnDayStarted(int day)
    {
        UpdateDayDisplay(day);
    }
    
    private void OnDayEnded(int day) { }
    
    public void ShowEndOfDayScreen()
    {
        if (endOfDayPanel != null)
            endOfDayPanel.SetActive(true);
    }
    
    public void CloseEndOfDayScreen()
    {
        if (endOfDayPanel != null)
            endOfDayPanel.SetActive(false);
    }
    
    public void ShowNotification(string message, float duration = 3f)
    {
        if (notificationPanel != null && notificationText != null)
        {
            notificationText.text = message;
            notificationPanel.SetActive(true);
            StartCoroutine(HideNotificationAfterDelay(duration));
        }
    }
    
    private IEnumerator HideNotificationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }
    
    public void OnStartDayButtonClicked()
    {
        if (Services.Game != null)
            Services.Game.StartDay();
        
        if (endOfDayPanel != null)
            endOfDayPanel.SetActive(false);
    }
    
    public void ShowUpgradeScreen()
    {
        if (upgradeUI != null)
            upgradeUI.Show();
    }
    
    public void CloseUpgradeScreen()
    {
        if (upgradeUI != null)
            upgradeUI.Hide();
    }
    
    public void ShowTooltip(string message, Vector2 position)
    {
        ShowNotification(message);
    }
    
    public void HideAllScreens()
    {
        CloseEndOfDayScreen();
        CloseUpgradeScreen();
        
        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }
    
    public void ShowNotification(string message)
    {
        ShowNotification(message, 3f);
    }
}