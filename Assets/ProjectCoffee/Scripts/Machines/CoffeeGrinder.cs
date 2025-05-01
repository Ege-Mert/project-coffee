using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Coffee grinder for creating ground coffee
/// </summary>
public class CoffeeGrinder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image beansLevelImage;
    [SerializeField] private Spinnable grindHandle;
    [SerializeField] private GroundCoffeeOutputZone groundCoffeeOutputZone;
    [SerializeField] private GameObject groundCoffeePrefab;
    
    [Header("Settings")]
    [SerializeField] private int maxBeanFills = 3;
    [SerializeField] private int spinsPerStage = 1;
    [SerializeField] private bool keepHandleAlwaysVisible = true;
    
    // [Header("Effects")]
    // [SerializeField] private ParticleSystem grindingParticles;
    // [SerializeField] private AudioSource grindSound;
    // [SerializeField] private Animator grinderAnimator;
    
    // State variables
    private int currentBeanFills = 0;
    private int spinsSinceLastUpgrade = 0;
    private GroundCoffee currentGroundCoffee;
    private bool isProcessingGrinding = false;
    
    private void Awake()
    {
        // Force Unity to print these messages to console
        print("CoffeeGrinderUI Awake called");
        if (groundCoffeePrefab == null)
        {
            print("ERROR: Ground Coffee Prefab is not assigned!");
        }
        
        if (groundCoffeeOutputZone == null)
        {
            print("ERROR: Ground Coffee Output Zone is not assigned!");
        }
    }
    
    private void Start()
    {
        print("CoffeeGrinderUI Start called");
        print($"Initial bean count: {currentBeanFills}");
        
        UpdateBeansVisual();
        
        // Extra validation
        if (groundCoffeePrefab == null)
        {
            print("ERROR: Ground Coffee Prefab is not assigned in the inspector!");
        }
        
        if (groundCoffeeOutputZone == null)
        {
            print("ERROR: Ground Coffee Output Zone is not assigned in the inspector!");
        }
        else
        {
            // Make sure the output zone has a reference to this grinder
            groundCoffeeOutputZone.SetParentGrinder(this);
            print($"Set parent grinder reference on {groundCoffeeOutputZone.name}");
        }
        
        if (grindHandle != null)
        {
            grindHandle.gameObject.SetActive(keepHandleAlwaysVisible || currentBeanFills > 0);
            grindHandle.OnSpinCompleted += OnHandleSpinCompleted;
            print("Subscribed to handle spin events");
        }
        else
        {
            print("ERROR: Grind Handle is not assigned in the inspector!");
        }
        
        // if (grindingParticles != null)
        // {
            // grindingParticles.Stop();
        // }
    }
    
    public void AddBeans(int amount)
    {
        print($">>> AddBeans({amount}) called. Current beans: {currentBeanFills}, Max: {maxBeanFills}");
        
        if (currentBeanFills < maxBeanFills)
        {
            int newAmount = Mathf.Min(currentBeanFills + amount, maxBeanFills);
            print($"Adding beans. Current: {currentBeanFills} -> New: {newAmount}");
            currentBeanFills = newAmount;
            
            UpdateBeansVisual();
            
            if (grindHandle != null && !grindHandle.gameObject.activeSelf)
            {
                grindHandle.gameObject.SetActive(true);
                print("Activated grind handle");
            }
            
            // Test direct access to variables
            print($"After adding - Bean count: {currentBeanFills}, Max: {maxBeanFills}");
            
            UIManager.Instance.ShowNotification($"Added beans. Total: {currentBeanFills}/{maxBeanFills}");
        }
        else
        {
            print($"Grinder already full. Current: {currentBeanFills}, Max: {maxBeanFills}");
            UIManager.Instance.ShowNotification("Grinder is full of beans!");
        }
    }
    
    private void OnHandleSpinCompleted(int spinCount)
    {
        print($">>> Handle spin completed. Spins: {spinsSinceLastUpgrade + 1}, Required: {spinsPerStage}, Beans: {currentBeanFills}");
        
        spinsSinceLastUpgrade++;
        
        // // Only play effects if there are beans
        // if (currentBeanFills > 0)
        // {
        //     if (grindingParticles != null && !grindingParticles.isPlaying)
        //     {
        //         grindingParticles.Play();
        //     }
            
        //     if (grindSound != null)
        //     {
        //         grindSound.Play();
        //     }
            
        //     if (grinderAnimator != null)
        //     {
        //         grinderAnimator.SetTrigger("Grind");
        //     }
        // }
        
        // Debug output regardless of bean count
        print($"Spin progress: {spinsSinceLastUpgrade}/{spinsPerStage} with {currentBeanFills} beans");
        
        // Check if we have enough spins to process grinding
        if (spinsSinceLastUpgrade >= spinsPerStage && currentBeanFills > 0 && !isProcessingGrinding)
        {
            print("Enough spins accumulated. Processing grinding...");
            ProcessGrinding();
            spinsSinceLastUpgrade = 0;
        }
        else if (currentBeanFills <= 0)
        {
            print("No beans to grind!");
            UIManager.Instance.ShowNotification("No beans to grind! Please add coffee beans.");
        }
    }
    
    private void ProcessGrinding()
    {
        if (currentBeanFills <= 0 || isProcessingGrinding)
            return;
            
        isProcessingGrinding = true;
        
        print($"Processing grinding. Beans: {currentBeanFills}, Has coffee: {currentGroundCoffee != null}");
        
        // Consume beans
        currentBeanFills--;
        UpdateBeansVisual();
        
        // If no ground coffee exists yet, create a new one
        if (currentGroundCoffee == null)
        {
            print("No current ground coffee found. Creating new ground coffee...");
            StartCoroutine(CreateGroundCoffee(GroundCoffee.GrindSize.Small));
        }
        // If ground coffee exists, upgrade its size
        else if (currentGroundCoffee != null)
        {
            print("Upgrading existing ground coffee size");
            UpgradeExistingCoffee();
        }
        
        // // Only stop particles if no beans, but don't hide the handle
        // if (currentBeanFills <= 0 && grindingParticles != null)
        // {
        //     grindingParticles.Stop();
        // }
    }
    
    private void UpgradeExistingCoffee()
    {
        if (currentGroundCoffee == null)
        {
            isProcessingGrinding = false;
            return;
        }
        
        // Get the current size before upgrading (for logging)
        GroundCoffee.GrindSize oldSize = currentGroundCoffee.GetGrindSize();
        
        // Upgrade the size
        currentGroundCoffee.UpgradeSize();
        
        // Get the new size
        GroundCoffee.GrindSize newSize = currentGroundCoffee.GetGrindSize();
        float amount = currentGroundCoffee.GetAmount();
        
        print($"Upgraded coffee from {oldSize} to {newSize}");
        
        if (oldSize == newSize)
        {
            print("WARNING: Coffee size didn't change after upgrade!");
        }
        
        // Show notification about the upgrade
        string sizeText = newSize.ToString();
        UIManager.Instance.ShowNotification($"Coffee ground size: {sizeText} ({amount}g)");
        
        // Add a visual effect to show the upgrade
        RectTransform coffeeRect = currentGroundCoffee.GetComponent<RectTransform>();
        if (coffeeRect != null)
        {
            coffeeRect.DOPunchScale(Vector3.one * 0.2f, 0.3f, 2, 0.5f);
        }
        
        isProcessingGrinding = false;
    }
    
    private IEnumerator CreateGroundCoffee(GroundCoffee.GrindSize size)
    {
        print("Creating ground coffee...");
        
        // Check required references
        if (groundCoffeePrefab == null)
        {
            print("ERROR: Cannot create ground coffee: groundCoffeePrefab is null!");
            isProcessingGrinding = false;
            yield break;
        }
        
        if (groundCoffeeOutputZone == null)
        {
            print("ERROR: Cannot create ground coffee: groundCoffeeOutputZone is null!");
            isProcessingGrinding = false;
            yield break;
        }
        
        // Wait for the end of frame to ensure UI elements are ready
        yield return new WaitForEndOfFrame();
        
        try
        {
            // Instantiate directly under the output zone
            GameObject coffeeObj = Instantiate(groundCoffeePrefab, groundCoffeeOutputZone.transform);
            
            if (coffeeObj == null)
            {
                print("ERROR: Failed to instantiate ground coffee prefab!");
                isProcessingGrinding = false;
                yield break;
            }
            
            // Get and initialize the component
            currentGroundCoffee = coffeeObj.GetComponent<GroundCoffee>();
            
            if (currentGroundCoffee == null)
            {
                print("ERROR: Ground coffee prefab does not have a GroundCoffee component!");
                Destroy(coffeeObj);
                isProcessingGrinding = false;
                yield break;
            }
            
            // Make sure it has a parent canvas
            Canvas parentCanvas = groundCoffeeOutputZone.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                print($"Found parent canvas: {parentCanvas.name}");
                
                // Update draggable UI reference directly
                Draggable draggable = coffeeObj.GetComponent<Draggable>();
                if (draggable != null)
                {
                    var field = typeof(Draggable).GetField("parentCanvas", 
                        System.Reflection.BindingFlags.Instance | 
                        System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Public);
                        
                    if (field != null)
                    {
                        field.SetValue(draggable, parentCanvas);
                        print($"Set canvas reference to {parentCanvas.name}");
                    }
                }
            }
            else
            {
                print("WARNING: No parent canvas found for ground coffee!");
            }
            
            // Setup the coffee
            print($"Setting grind size to {size}");
            currentGroundCoffee.SetGrindSize(size);
            
            // Position properly
            RectTransform coffeeRect = coffeeObj.GetComponent<RectTransform>();
            if (coffeeRect != null)
            {
                coffeeRect.anchoredPosition = Vector2.zero;
                
                // Add appearance animation
                coffeeRect.localScale = Vector3.zero;
                coffeeRect.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
                
                print("Ground coffee created and positioned successfully!");
            }
            else
            {
                print("ERROR: Ground coffee prefab does not have a RectTransform component!");
                Destroy(coffeeObj);
                isProcessingGrinding = false;
                yield break;
            }
            
            UIManager.Instance.ShowNotification("Ground coffee created!");
        }
        catch (System.Exception e)
        {
            print($"ERROR creating ground coffee: {e.Message}");
        }
        
        isProcessingGrinding = false;
    }
    
    private void UpdateBeansVisual()
    {
        print($"Updating beans visual. Current: {currentBeanFills}, Max: {maxBeanFills}");
        
        if (beansLevelImage != null)
        {
            float fillAmount = (float)currentBeanFills / maxBeanFills;
            print($"Setting fill amount to {fillAmount}");
            beansLevelImage.fillAmount = fillAmount;
        }
        else
        {
            print("WARNING: beansLevelImage is null!");
        }
    }
    
    public void OnGroundCoffeeRemoved()
    {
        print("Ground coffee removed from output zone");
        currentGroundCoffee = null;
    }
    
    // For debuggering - add a test button to call this
    public void TestAddBean()
    {
        print("Test button: Adding 1 bean");
        AddBeans(1);
    }
    
    // For debugging - shows how many beans are currently in the grinder
    public void DebugShowBeanCount()
    {
        print($"DEBUG: Current bean count is {currentBeanFills}");
        UIManager.Instance.ShowNotification($"Bean count: {currentBeanFills}/{maxBeanFills}");
    }
}