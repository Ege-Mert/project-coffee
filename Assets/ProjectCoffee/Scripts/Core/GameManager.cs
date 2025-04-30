using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
/// <summary>
/// Main game manager
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance => _instance;
    
    [SerializeField] private float dayLengthInSeconds = 300f; // 5 minutes per day
    [SerializeField] private int startingMoney = 100;
    
    private int money;
    private float dayTimer;
    private int currentDay = 1;
    private bool isDayActive = false;
    
    public int Money => money;
    public float DayTimeRemaining => dayLengthInSeconds - dayTimer;
    public int CurrentDay => currentDay;
    public bool IsDayActive => isDayActive;
    
    public event Action<int> OnMoneyChanged;
    public event Action<int> OnDayStarted;
    public event Action<int> OnDayEnded;
    
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
        OnDayStarted?.Invoke(currentDay);
    }
    
    public void EndDay()
    {
        isDayActive = false;
        currentDay++;
        OnDayEnded?.Invoke(currentDay - 1);
        
        // Show end of day screen
        UIManager.Instance.ShowEndOfDayScreen();
    }
    
    public void AddMoney(int amount)
    {
        money += amount;
        OnMoneyChanged?.Invoke(money);
    }
    
    public bool TrySpendMoney(int amount)
    {
        if (money >= amount)
        {
            money -= amount;
            OnMoneyChanged?.Invoke(money);
            return true;
        }
        
        return false;
    }
}