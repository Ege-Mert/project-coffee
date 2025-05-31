using UnityEngine;
using ProjectCoffee.Services;
using ProjectCoffee.Services.Interfaces;
using ProjectCoffee.Machines;
using ProjectCoffee.Core;
using System.Collections;
using CoreServices = ProjectCoffee.Core.Services;

namespace ProjectCoffee.Machines
{
    public class CoffeeGrammingMachine : MachineBase
    {
        [Header("Gramming Machine Components")]
        [SerializeField] private DropZone portafilterZone;
        [SerializeField] private DropZone groundCoffeeZone;
        
        private CoffeeGrammingService service;
        private Portafilter currentPortafilter;
        
        protected override void InitializeMachine()
        {
            service = new CoffeeGrammingService(config as GrammingMachineConfig);
            ServiceManager.Instance?.RegisterMachineService<IGrammingService>(service);
            
            SetupServiceEvents();
            ConfigureDropZones();
        }
        
        private void SetupServiceEvents()
        {
            if (service != null)
            {
                service.OnStateChanged += TransitionToState;
                service.OnProgressChanged += UpdateProgress;
                service.OnProcessCompleted += CompleteProcess;
                service.OnAutoDoseCompleted += OnAutoDoseCompleted;
                service.OnAutoDoseStarted += OnAutoDoseStarted;
            }
        }
        
        private void ConfigureDropZones()
        {
            if (portafilterZone != null)
            {
                portafilterZone.AcceptPredicate = (item) => {
                    bool isPortafilter = item is Portafilter;
                    bool canAccept = isPortafilter && !service.HasPortafilter;
                    return canAccept;
                };
            }
            
            if (groundCoffeeZone != null)
            {
                groundCoffeeZone.AcceptPredicate = (item) => {
                    return item is GroundCoffee;
                };
            }
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            if (service != null)
            {
                service.OnStateChanged -= TransitionToState;
                service.OnProgressChanged -= UpdateProgress;
                service.OnProcessCompleted -= CompleteProcess;
                service.OnAutoDoseCompleted -= OnAutoDoseCompleted;
                service.OnAutoDoseStarted -= OnAutoDoseStarted;
            }
        }
        
        protected override void ApplyUpgrade(int previousLevel, int newLevel)
        {
            service?.SetUpgradeLevel(newLevel);
            
            switch (newLevel)
            {
                case 1:
                    NotifyUser("Upgraded to Semi-Auto gramming - Press button once for perfect 18g dose");
                    break;
                case 2:
                    NotifyUser("Upgraded to Fully Automatic gramming - Automatically doses when portafilter is placed");
                    CheckForAutoDosing();
                    break;
            }
        }
        
        private void Update()
        {
            CheckPortafilterPresence();
        }
        
        private void CheckPortafilterPresence()
        {
            if (portafilterZone == null) return;
            
            if (portafilterZone.transform.childCount > 0 && currentPortafilter == null)
            {
                Portafilter portafilter = portafilterZone.transform.GetChild(0).GetComponent<Portafilter>();
                if (portafilter != null)
                {
                    currentPortafilter = portafilter;
                    service.SetPortafilterPresent(true);
                    
                    if (upgradeLevel == 2 && service.StoredCoffeeAmount > 0)
                    {
                        Invoke("CheckForAutoDosing", 0.5f);
                    }
                }
            }
            else if (portafilterZone.transform.childCount == 0 && currentPortafilter != null)
            {
                currentPortafilter = null;
                service.SetPortafilterPresent(false);
            }
        }
        
        private void CheckForAutoDosing()
        {
            if (upgradeLevel == 2 && currentPortafilter != null && service.StoredCoffeeAmount > 0)
            {
                if (CurrentState != ProjectCoffee.Services.MachineState.Ready)
                {
                    service.TransitionToState(ProjectCoffee.Services.MachineState.Ready);
                }
                
                Invoke("DirectAutoDosingCheck", 0.5f);
            }
        }
        
        public void OnGroundCoffeeDropped(Draggable item)
        {
            if (item is GroundCoffee groundCoffee)
            {
                float coffeeAmount = groundCoffee.GetAmount();
                bool added = service.AddCoffee(coffeeAmount);
                
                Destroy(groundCoffee.gameObject, 0.5f);
                
                if (upgradeLevel == 2 && currentPortafilter != null)
                {
                    Invoke("CheckForAutoDosing", 0.5f);
                }
            }
        }
        
        public void OnPortafilterDropped(Draggable item)
        {
            if (item is Portafilter portafilter)
            {
                currentPortafilter = portafilter;
                service.SetPortafilterPresent(true);
                
                if (upgradeLevel == 2 && service.StoredCoffeeAmount > 0)
                {
                    Invoke("CheckForAutoDosing", 0.5f);
                }
            }
        }
        
        public void OnPortafilterRemoved(Draggable item)
        {
            if (item is Portafilter)
            {
                currentPortafilter = null;
                service.SetPortafilterPresent(false);
            }
        }
        
        public override void StartProcess()
        {
            base.StartProcess();
            
            switch (GetInteractionType())
            {
                case InteractionType.ManualLever:
                    HandleManualOperation();
                    break;
                case InteractionType.ButtonPress:
                    StartCoroutine(AutoDoseProcess());
                    break;
                case InteractionType.AutoProcess:
                    StartCoroutine(AutoDoseProcess());
                    break;
            }
        }
        
        private void HandleManualOperation()
        {
            if (service.HasPortafilter && service.StoredCoffeeAmount > 0)
            {
                service.StartProcessing();
            }
        }
        
        public void OnLeverSpinCompleted()
        {
            if (GetInteractionType() == InteractionType.ManualLever)
            {
                service.OnDispensingHold(0.1f);
                service.OnDispensingRelease();
            }
        }
        
        public void OnGrammingButtonHold(float duration)
        {
            if (service != null && service.HasPortafilter && service.StoredCoffeeAmount > 0)
            {
                float previousAmount = service.PortafilterCoffeeAmount;
                service.OnDispensingHold(Time.deltaTime);
                float newAmount = service.PortafilterCoffeeAmount;
                
                if (currentPortafilter != null && newAmount != previousAmount)
                {
                    currentPortafilter.Clear();
                    currentPortafilter.TryAddItem("ground_coffee", newAmount);
                }
            }
        }
        
        public void OnGrammingButtonRelease(float heldDuration)
        {
            service?.OnDispensingRelease();
        }
        
        public void OnAutoDoseButtonClicked()
        {
            if (GetInteractionType() == InteractionType.ButtonPress)
            {
                StartProcess();
            }
        }
        
        private IEnumerator AutoDoseProcess()
        {
            if (service == null || !service.HasPortafilter || service.StoredCoffeeAmount <= 0)
                yield break;
            
            service.StartProcessing();
            
            float processingTime = GetProcessTime();
            float startTime = Time.time;
            
            while (Time.time < startTime + processingTime)
            {
                float progress = (Time.time - startTime) / processingTime;
                UpdateProgress(progress);
                yield return null;
            }
            
            service.PerformAutoDose();
            
            if (currentPortafilter != null)
            {
                float newAmount = service.PortafilterCoffeeAmount;
                currentPortafilter.Clear();
                currentPortafilter.TryAddItem("ground_coffee", newAmount);
            }
            
            yield return new WaitForSeconds(0.5f);
        }
        
        private void OnAutoDoseCompleted()
        {
            if (currentPortafilter != null && service != null)
            {
                float newAmount = service.PortafilterCoffeeAmount;
                currentPortafilter.Clear();
                currentPortafilter.TryAddItem("ground_coffee", newAmount);
            }
        }
        
        private void OnAutoDoseStarted()
        {
            // Handled by service
        }
        
        private void DirectAutoDosingCheck()
        {
            if (service.CurrentState != ProjectCoffee.Services.MachineState.Ready)
            {
                if (service.CurrentState == ProjectCoffee.Services.MachineState.Idle)
                    service.TransitionToState(ProjectCoffee.Services.MachineState.Ready);
            }
            
            StartCoroutine(ForceAutoDosing());
        }
        
        private IEnumerator ForceAutoDosing()
        {
            yield return new WaitForSeconds(0.2f);
            
            if (service != null && upgradeLevel == 2 && 
                currentPortafilter != null && service.HasPortafilter && 
                service.StoredCoffeeAmount > 0)
            {
                var grammingConfig = config as GrammingMachineConfig;
                float idealAmount = grammingConfig?.idealGramAmount ?? 18f;
                float currentAmount = service.PortafilterCoffeeAmount;
                float amountNeeded = idealAmount - currentAmount;
                
                if (amountNeeded > 0)
                {
                    StartProcess();
                }
            }
        }
        
        public bool HasPortafilter => service?.HasPortafilter ?? false;
        public float StoredCoffeeAmount => service?.StoredCoffeeAmount ?? 0f;
        public float PortafilterCoffeeAmount => service?.PortafilterCoffeeAmount ?? 0f;
        public Portafilter CurrentPortafilter => currentPortafilter;
        
        public CoffeeGrammingService GetService() => service;
    }
}
