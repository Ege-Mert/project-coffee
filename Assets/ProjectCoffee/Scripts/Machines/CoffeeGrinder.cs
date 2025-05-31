using UnityEngine;
using ProjectCoffee.Services;
using ProjectCoffee.Services.Interfaces;
using ProjectCoffee.Machines;
using ProjectCoffee.Core;
using System.Collections;
using CoreServices = ProjectCoffee.Core.Services;

namespace ProjectCoffee.Machines
{
    public class CoffeeGrinder : MachineBase
    {
        [Header("Grinder Components")]
        [SerializeField] private GroundCoffeeOutputZone groundCoffeeOutputZone;
        [SerializeField] private GameObject groundCoffeePrefab;
        
        private CoffeeGrinderService service;
        private GroundCoffee currentGroundCoffee;
        private int currentBeans = 0;
        
        protected override void InitializeMachine()
        {
            service = new CoffeeGrinderService(config as GrinderConfig);
            ServiceManager.Instance?.RegisterMachineService<IGrinderService>(service);
            
            SetupServiceEvents();
            SetupOutputZone();
        }
        
        private void SetupServiceEvents()
        {
            if (service != null)
            {
                service.OnStateChanged += TransitionToState;
                service.OnProgressChanged += UpdateProgress;
                service.OnProcessCompleted += CompleteProcess;
                service.OnCoffeeOutputReady += CreateGroundCoffee;
                service.OnCoffeeSizeUpgraded += UpgradeGroundCoffee;
            }
        }
        
        private void SetupOutputZone()
        {
            if (groundCoffeeOutputZone != null)
                groundCoffeeOutputZone.SetParentGrinder(this);
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            if (service != null)
            {
                service.OnStateChanged -= TransitionToState;
                service.OnProgressChanged -= UpdateProgress;
                service.OnProcessCompleted -= CompleteProcess;
                service.OnCoffeeOutputReady -= CreateGroundCoffee;
                service.OnCoffeeSizeUpgraded -= UpgradeGroundCoffee;
            }
        }
        
        protected override void ApplyUpgrade(int previousLevel, int newLevel)
        {
            service?.SetUpgradeLevel(newLevel);
            
            switch (newLevel)
            {
                case 1:
                    NotifyUser("Grinder now uses button operation!");
                    break;
                case 2:
                    NotifyUser("Grinder is now fully automatic!");
                    break;
            }
        }
        
        private void LateUpdate()
        {
            if (service == null || upgradeLevel != 2) return;
                
            EnsureAutomaticProcessing();
            
            if (service.CurrentState == ProjectCoffee.Services.MachineState.Processing)
                service.ProcessUpdate(Time.deltaTime);
        }
        
        public void AddBeans(int amount)
        {
            currentBeans += amount;
            service?.AddBeans(amount);
            
            if (currentBeans > 0 && CurrentState == ProjectCoffee.Services.MachineState.Idle)
            {
                TransitionToState(ProjectCoffee.Services.MachineState.Ready);
            }
        }
        
        public void OnHandleSpinCompleted()
        {
            Debug.Log($"CoffeeGrinder: OnHandleSpinCompleted called. UpgradeLevel: {upgradeLevel}, InteractionType: {GetInteractionType()}");
            
            // Fixed: Check for upgrade level 0 directly instead of interaction type
            // This ensures manual handle spinning works at level 0
            if (upgradeLevel == 0)
            {
                Debug.Log("CoffeeGrinder: Processing handle spin for level 0");
                service?.OnHandleSpinCompleted();
            }
            else
            {
                Debug.Log($"CoffeeGrinder: Handle spin ignored for upgrade level {upgradeLevel}");
            }
        }
        
        public void OnButtonPress()
        {
            Debug.Log($"CoffeeGrinder: OnButtonPress called. InteractionType: {GetInteractionType()}");
            
            if (GetInteractionType() == InteractionType.ButtonPress)
            {
                StartGrindingProcess();
            }
        }
        
        public override void StartProcess()
        {
            base.StartProcess();
            StartGrindingProcess();
        }
        
        private void StartGrindingProcess()
        {
            if (service == null || !service.HasBeans) return;
            
            switch (GetInteractionType())
            {
                case InteractionType.ButtonPress:
                case InteractionType.AutoProcess:
                    StartCoroutine(ProcessGrinding());
                    break;
            }
        }
        
        private IEnumerator ProcessGrinding()
        {
            while (service != null && service.HasBeans)
            {
                if (currentGroundCoffee != null && 
                    currentGroundCoffee.GetGrindSize() == GroundCoffee.GrindSize.Large)
                    break;
                
                service.OnButtonPressed();
                
                float grindTime = GetProcessTime();
                float startTime = Time.time;
                
                while (Time.time < startTime + grindTime)
                {
                    float progress = (Time.time - startTime) / grindTime;
                    UpdateProgress(progress);
                    service.ProcessUpdate(Time.deltaTime);
                    yield return null;
                }
                
                if (service.HasBeans)
                {
                    service.ForceReadyState();
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }
        
        private void EnsureAutomaticProcessing()
        {
            if (!service.HasBeans || service.CurrentState == ProjectCoffee.Services.MachineState.Processing)
                return;
                
            if (service.CurrentState != ProjectCoffee.Services.MachineState.Ready)
                service.ForceReadyState();
            
            service.CheckAutoProcess();
        }
        
        private void CreateGroundCoffee(GroundCoffee.GrindSize size)
        {
            Debug.Log($"CoffeeGrinder: Creating ground coffee with size {size}");
            
            if (groundCoffeePrefab == null || groundCoffeeOutputZone == null)
            {
                Debug.LogError("Cannot create ground coffee: Missing prefab or output zone!");
                return;
            }
            
            if (currentGroundCoffee != null)
            {
                UpgradeGroundCoffee(size);
                return;
            }
            
            StartCoroutine(CreateGroundCoffeeCoroutine(size));
        }
        
        private IEnumerator CreateGroundCoffeeCoroutine(GroundCoffee.GrindSize size)
        {
            yield return new WaitForEndOfFrame();
            
            GameObject coffeeObj = Instantiate(groundCoffeePrefab, groundCoffeeOutputZone.transform);
            currentGroundCoffee = coffeeObj.GetComponent<GroundCoffee>();
            
            if (currentGroundCoffee != null)
            {
                currentGroundCoffee.SetGrindSize(size);
                currentGroundCoffee.SetAmount(GetAmountForSize(size));
                
                RectTransform rectTransform = coffeeObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                    rectTransform.anchoredPosition = Vector2.zero;
                    
                Debug.Log($"CoffeeGrinder: Ground coffee created successfully with size {size}");
            }
            else
            {
                Debug.LogError("CoffeeGrinder: Failed to get GroundCoffee component from instantiated object!");
            }
        }
        
        private void UpgradeGroundCoffee(GroundCoffee.GrindSize newSize)
        {
            Debug.Log($"CoffeeGrinder: Upgrading ground coffee to size {newSize}");
            
            if (currentGroundCoffee == null)
            {
                CreateGroundCoffee(newSize);
                return;
            }
            
            currentGroundCoffee.SetGrindSize(newSize);
            currentGroundCoffee.SetAmount(GetAmountForSize(newSize));
        }
        
        private float GetAmountForSize(GroundCoffee.GrindSize size)
        {
            if (config is GrinderConfig grinderConfig)
            {
                switch (size)
                {
                    case GroundCoffee.GrindSize.Small:
                        return grinderConfig.groundCoffeeSizes.Length > 0 ? grinderConfig.groundCoffeeSizes[0] : 6f;
                    case GroundCoffee.GrindSize.Medium:
                        return grinderConfig.groundCoffeeSizes.Length > 1 ? grinderConfig.groundCoffeeSizes[1] : 12f;
                    case GroundCoffee.GrindSize.Large:
                        return grinderConfig.groundCoffeeSizes.Length > 2 ? grinderConfig.groundCoffeeSizes[2] : 18f;
                    default:
                        return 6f;
                }
            }
            return 6f;
        }
        
        public void OnGroundCoffeeRemoved()
        {
            Debug.Log("CoffeeGrinder: Ground coffee removed");
            currentGroundCoffee = null;
            service?.OnGroundCoffeeRemoved();
        }
        
        public bool CanAddBeans => service?.CanAddBeans ?? false;
        public int CurrentBeanCount => service?.CurrentBeanFills ?? 0;
        public int MaxBeanCount => service?.MaxBeanFills ?? 3;
        
        public CoffeeGrinderService GetService() => service;
    }
}