using UnityEngine;
using ProjectCoffee.Services;
using ProjectCoffee.Services.Interfaces;
using ProjectCoffee.Services.Grinder;
using ProjectCoffee.Machines;
using ProjectCoffee.Core;
using System.Collections;

namespace ProjectCoffee.Machines.Grinder
{
    /// <summary>
    /// Simplified grinder machine that focuses on Unity integration and component coordination
    /// </summary>
    public class GrinderMachine : MachineBase
    {
        [Header("Grinder Components")]
        [SerializeField] private GroundCoffeeOutputZone groundCoffeeOutputZone;
        [SerializeField] private GameObject groundCoffeePrefab;
        
        private GrinderService service;
        private GroundCoffee currentGroundCoffee;
        
        #region Properties
        
        public bool CanAddBeans => service?.CanAddBeans ?? false;
        public int CurrentBeanCount => service?.CurrentBeanFills ?? 0;
        public int MaxBeanCount => service?.MaxBeanFills ?? 3;
        public GrinderService GetService() => service;
        
        #endregion
        
        #region Machine Lifecycle
        
        protected override void InitializeMachine()
        {
            InitializeService();
            SetupComponents();
            SubscribeToEvents();
        }
        
        private void InitializeService()
        {
            service = new GrinderService(config as GrinderConfig);
            ServiceManager.Instance?.RegisterMachineService<IGrinderService>(service);
            
            Debug.Log($"GrinderMachine: Service initialized with config: {config?.name}");
        }
        
        private void SetupComponents()
        {
            if (groundCoffeeOutputZone != null)
            {
                groundCoffeeOutputZone.SetParentGrinder(this);
                Debug.Log("GrinderMachine: Output zone configured");
            }
            else
            {
                Debug.LogError("GrinderMachine: Ground coffee output zone not assigned!");
            }
        }
        
        private void SubscribeToEvents()
        {
            if (service != null)
            {
                service.OnStateChanged += TransitionToState;
                service.OnProgressChanged += UpdateProgress;
                service.OnProcessCompleted += CompleteProcess;
                service.OnCoffeeOutputReady += CreateGroundCoffee;
                service.OnCoffeeSizeUpgraded += UpgradeGroundCoffee;
                
                Debug.Log("GrinderMachine: Event subscriptions completed");
            }
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromEvents();
        }
        
        private void UnsubscribeFromEvents()
        {
            if (service != null)
            {
                service.OnStateChanged -= TransitionToState;
                service.OnProgressChanged -= UpdateProgress;
                service.OnProcessCompleted -= CompleteProcess;
                service.OnCoffeeOutputReady -= CreateGroundCoffee;
                service.OnCoffeeSizeUpgraded -= UpgradeGroundCoffee;
            }
        }
        
        #endregion
        
        #region Unity Updates
        
        private void LateUpdate()
        {
            if (service == null) return;
            
            // Always call ProcessUpdate to handle both processing and auto-process timing
            service.ProcessUpdate(Time.deltaTime);
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Add beans to the grinder
        /// </summary>
        public void AddBeans(int amount)
        {
            Debug.Log($"GrinderMachine: Adding {amount} beans");
            service?.AddBeans(amount);
            
            // Trigger auto-processing check for level 2 when beans are added
            if (upgradeLevel == 2 && service != null)
            {
                service.CheckAutoProcess();
            }
        }
        
        /// <summary>
        /// Handle manual handle spin completion
        /// </summary>
        public void OnHandleSpinCompleted()
        {
            Debug.Log($"GrinderMachine: Handle spin completed. UpgradeLevel: {upgradeLevel}");
            
            if (upgradeLevel == 0)
            {
                Debug.Log("GrinderMachine: Processing handle spin for level 0");
                service?.OnHandleSpinCompleted();
            }
            else
            {
                Debug.Log($"GrinderMachine: Handle spin ignored for upgrade level {upgradeLevel}");
            }
        }
        
        /// <summary>
        /// Handle button press
        /// </summary>
        public void OnButtonPress()
        {
            Debug.Log($"GrinderMachine: Button pressed. UpgradeLevel: {upgradeLevel}");
            
            if (upgradeLevel == 1)
            {
                service?.OnButtonPressed();
            }
        }
        
        /// <summary>
        /// Handle ground coffee removal
        /// </summary>
        public void OnGroundCoffeeRemoved()
        {
            Debug.Log("GrinderMachine: Ground coffee removed");
            currentGroundCoffee = null;
            service?.OnGroundCoffeeRemoved();
        }
        
        /// <summary>
        /// Stop continuous processing (for level 1)
        /// </summary>
        public void StopContinuousProcessing()
        {
            Debug.Log("GrinderMachine: Stopping continuous processing");
            service?.StopContinuousProcessing();
        }
        
        #endregion
        
        #region MachineBase Overrides
        
        public override void StartProcess()
        {
            base.StartProcess();
            Debug.Log("GrinderMachine: Start process called");
            service?.OnButtonPressed();
        }
        
        protected override void ApplyUpgrade(int previousLevel, int newLevel)
        {
            service?.SetUpgradeLevel(newLevel);
            
            string message = newLevel switch
            {
                1 => "Grinder now uses button operation!",
                2 => "Grinder is now fully automatic!",
                _ => $"Grinder upgraded to level {newLevel}!"
            };
            
            NotifyUser(message);
            Debug.Log($"GrinderMachine: Upgrade applied - {message}");
        }
        
        #endregion
        
        #region Ground Coffee Management
        
        /// <summary>
        /// Create new ground coffee
        /// </summary>
        private void CreateGroundCoffee(GroundCoffee.GrindSize size)
        {
            Debug.Log($"GrinderMachine: Creating ground coffee with size {size}");
            
            if (!ValidateGroundCoffeeCreation())
                return;
            
            if (currentGroundCoffee != null)
            {
                UpgradeGroundCoffee(size);
                return;
            }
            
            StartCoroutine(CreateGroundCoffeeCoroutine(size));
        }
        
        /// <summary>
        /// Upgrade existing ground coffee
        /// </summary>
        private void UpgradeGroundCoffee(GroundCoffee.GrindSize newSize)
        {
            Debug.Log($"GrinderMachine: Upgrading ground coffee to size {newSize}");
            
            if (currentGroundCoffee == null)
            {
                CreateGroundCoffee(newSize);
                return;
            }
            
            currentGroundCoffee.SetGrindSize(newSize);
            
            // Get amount from service's logic
            var grindConfig = config as GrinderConfig;
            float amount = GetAmountForSize(newSize, grindConfig);
            currentGroundCoffee.SetAmount(amount);
        }
        
        /// <summary>
        /// Coroutine to create ground coffee with proper timing
        /// </summary>
        private IEnumerator CreateGroundCoffeeCoroutine(GroundCoffee.GrindSize size)
        {
            yield return new WaitForEndOfFrame();
            
            GameObject coffeeObj = Instantiate(groundCoffeePrefab, groundCoffeeOutputZone.transform);
            currentGroundCoffee = coffeeObj.GetComponent<GroundCoffee>();
            
            if (currentGroundCoffee != null)
            {
                SetupGroundCoffee(size);
                PositionGroundCoffee(coffeeObj);
                
                Debug.Log($"GrinderMachine: Ground coffee created successfully with size {size}");
            }
            else
            {
                Debug.LogError("GrinderMachine: Failed to get GroundCoffee component from instantiated object!");
                Destroy(coffeeObj);
            }
        }
        
        /// <summary>
        /// Setup ground coffee properties
        /// </summary>
        private void SetupGroundCoffee(GroundCoffee.GrindSize size)
        {
            currentGroundCoffee.SetGrindSize(size);
            
            var grindConfig = config as GrinderConfig;
            float amount = GetAmountForSize(size, grindConfig);
            currentGroundCoffee.SetAmount(amount);
        }
        
        /// <summary>
        /// Position ground coffee in output zone
        /// </summary>
        private void PositionGroundCoffee(GameObject coffeeObj)
        {
            RectTransform rectTransform = coffeeObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.zero;
            }
        }
        
        /// <summary>
        /// Validate that ground coffee can be created
        /// </summary>
        private bool ValidateGroundCoffeeCreation()
        {
            if (groundCoffeePrefab == null)
            {
                Debug.LogError("GrinderMachine: Cannot create ground coffee - Missing prefab!");
                return false;
            }
            
            if (groundCoffeeOutputZone == null)
            {
                Debug.LogError("GrinderMachine: Cannot create ground coffee - Missing output zone!");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Get amount for grind size from config
        /// </summary>
        private float GetAmountForSize(GroundCoffee.GrindSize size, GrinderConfig grindConfig)
        {
            if (grindConfig?.groundCoffeeSizes != null && (int)size < grindConfig.groundCoffeeSizes.Length)
            {
                return grindConfig.groundCoffeeSizes[(int)size];
            }
            
            // Fallback values
            return size switch
            {
                GroundCoffee.GrindSize.Small => 6f,
                GroundCoffee.GrindSize.Medium => 12f,
                GroundCoffee.GrindSize.Large => 18f,
                _ => 6f
            };
        }
        
        #endregion
    }
}
