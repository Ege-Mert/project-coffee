using System;
using UnityEngine;
using ProjectCoffee.Core;
using ProjectCoffee.Services.Interfaces;

public class GameManager : MonoBehaviour, IGameService
{
    private static GameManager _instance;
    public static GameManager Instance => _instance;
    
    [SerializeField] private float dayLengthInSeconds = 300f;
    [SerializeField] private int startingMoney = 100;
    
    private int money;
    private float dayTimer;
    private int currentDay = 1;
    private bool isDayActive = false;
    
    public int Money => money;
    public float DayTimeRemaining => dayLengthInSeconds - dayTimer;
    public int CurrentDay => currentDay;
    public bool IsDayActive => isDayActive;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        money = startingMoney;
    }
    
    private void Update()
    {
        if (isDayActive)
        {
            dayTimer += Time.deltaTime;
            
            if (dayTimer >= dayLengthInSeconds)
            {
                EndDay();
            }
        }
    }
    
    public void StartDay()
    {
        isDayActive = true;
        dayTimer = 0;
        EventBus.NotifyDayStarted(currentDay);
    }
    
    public void EndDay()
    {
        isDayActive = false;
        currentDay++;
        EventBus.NotifyDayEnded(currentDay - 1);
        
        if (Services.UI != null)
        {
            Services.UI.ShowEndOfDayScreen();
            Services.UI.ShowUpgradeScreen();
        }
        else
        {
            Debug.LogError("Cannot show end of day screens: No UI service available");
        }
    }
    
    public void AddMoney(int amount)
    {
        money += amount;
        EventBus.NotifyMoneyChanged(money);
    }
    
    public bool TrySpendMoney(int amount)
    {
        if (money >= amount)
        {
            money -= amount;
            EventBus.NotifyMoneyChanged(money);
            return true;
        }
        
        return false;
    }
}