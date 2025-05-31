using UnityEngine;
using ProjectCoffee.Services;
using ProjectCoffee.Machines;
using System.Collections;

namespace ProjectCoffee.Machines
{
    /// <summary>
    /// Coffee grinder machine that delegates all logic to service (REFACTORED VERSION)
    /// </summary>
    public class CoffeeGrinderRefactored : Machine<CoffeeGrinderService, GrinderConfig>
    {
        [Header("Grinder Components")]
        [SerializeField] private GroundCoffeeOutputZone groundCoffeeOutputZone;
        [SerializeField] private GameObject groundCoffeePrefab;
        
        private GroundCoffee currentGroundCoffee;
        
        // Properties for UI/external access
        public bool HasBeans => service?.HasBeans ?? false;
        public bool CanGrind => service?.CanProcess() ?? false;
        public int CurrentBeanCount => service?.CurrentBeanFills ?? 0;
        public int MaxBeanCount => service?.MaxBeanFills ?? 3;
        
        protected override void InitializeService()
        {
            service = new CoffeeGrinderService(config as GrinderConfig);
        }
        
        protected override void Start()
        {
            base.Start();
            
            SetupOutputZone();
            
            // Subscribe to grinder-specific events
            if (service != null)
            {
                service.OnCoffeeOutputReady += CreateGroundCoffee;
                service.OnCoffeeSizeUpgraded += UpgradeGroundCoffee;
            }
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            if (service != null)
            {
                service.OnCoffeeOutputReady -= CreateGroundCoffee;
                service.OnCoffeeSizeUpgraded -= UpgradeGroundCoffee;
            }
        }
        
        private void SetupOutputZone()
        {
            if (groundCoffeeOutputZone != null)
            {
                groundCoffeeOutputZone.SetParentGrinder(this);
            }
        }
        
        private void Update()
        {
            // For level 2 automatic processing
            if (service?.UpgradeLevel == 2)
            {
                EnsureAutomaticProcessing();
                
                if (service.CurrentState == MachineState.Processing)
                {
                    service.ProcessUpdate(Time.deltaTime);
                }
            }
        }
        
        /// <summary>
        /// Add beans to the grinder
        /// </summary>
        public void AddBeans(int amount)
        {
            service?.AddBeans(amount);
        }
        
        /// <summary>
        /// Handle manual spin completion
        /// </summary>
        public void OnHandleSpinCompleted(int spinCount)
        {
            service?.OnHandleSpinCompleted();
        }
        
        /// <summary>
        /// Handle grind button click
        /// </summary>
        public void GrindButtonClick()
        {
            if (service == null || !service.HasBeans) return;
            
            StartCoroutine(ProcessGrinding());
        }
        
        private IEnumerator ProcessGrinding()
        {
            // Process ALL beans continuously for level 1
            while (service != null && service.HasBeans)
            {
                // Check if coffee became max size during processing
                if (currentGroundCoffee != null && 
                    currentGroundCoffee.GetGrindSize() == GroundCoffee.GrindSize.Large)
                {
                    break;
                }
                
                service.OnButtonPressed();
                
                // Get grind time from config
                float grindTime = 3f;
                if (config is GrinderConfig grinderConfig)
                {
                    grindTime = service.UpgradeLevel >= 2 ? 
                        grinderConfig.level2GrindTime : 
                        grinderConfig.level1GrindTime;
                }
                
                float startTime = Time.time;
                while (Time.time < startTime + grindTime)
                {
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
        
        private void CreateGroundCoffee(GroundCoffee.GrindSize size)
        {
            if (groundCoffeePrefab == null || groundCoffeeOutputZone == null) return;
            
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
                {
                    rectTransform.anchoredPosition = Vector2.zero;
                }
            }
        }
        
        private void UpgradeGroundCoffee(GroundCoffee.GrindSize newSize)
        {
            if (currentGroundCoffee == null) return;
            
            currentGroundCoffee.SetGrindSize(newSize);
            currentGroundCoffee.SetAmount(GetAmountForSize(newSize));
        }
        
        private float GetAmountForSize(GroundCoffee.GrindSize size)
        {
            if (config != null && config is GrinderConfig grinderConfig)
            {
                switch (size)
                {
                    case GroundCoffee.GrindSize.Small:
                        return grinderConfig.groundCoffeeSizes.Length > 0 ? grinderConfig.groundCoffeeSizes[0] : 6f;
                    case GroundCoffee.GrindSize.Medium:
                        return grinderConfig.groundCoffeeSizes.Length > 1 ? grinderConfig.groundCoffeeSizes[1] : 12f;
                    case GroundCoffee.GrindSize.Large:
                        return grinderConfig.groundCoffeeSizes.Length > 2 ? grinderConfig.groundCoffeeSizes[2] : 18f;
                }
            }
            return 6f;
        }
        
        private void EnsureAutomaticProcessing()
        {
            if (!service.HasBeans || service.CurrentState == MachineState.Processing)
                return;
                
            if (service.CurrentState != MachineState.Ready)
            {
                service.ForceReadyState();
            }
            
            service.CheckAutoProcess();
        }
        
        public void OnGroundCoffeeRemoved()
        {
            currentGroundCoffee = null;
            service?.OnGroundCoffeeRemoved();
        }
    }
}