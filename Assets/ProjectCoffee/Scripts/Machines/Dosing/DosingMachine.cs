using UnityEngine;
using ProjectCoffee.Services.Dosing;
using ProjectCoffee.Services.Interfaces;
using ProjectCoffee.Services;
using ProjectCoffee.Machines;
using ProjectCoffee.Core;
using ProjectCoffee.Machines.Dosing.Logic;
using System.Collections;

namespace ProjectCoffee.Machines.Dosing
{
    /// <summary>
    /// Clean dosing machine focused only on Unity integration.
    /// All business logic moved to DosingService and DosingLogic.
    /// </summary>
    public class DosingMachine : MachineBase
    {
        [Header("Dosing Machine Components")]
        [SerializeField] private DropZone portafilterZone;
        [SerializeField] private DropZone groundCoffeeZone;
        
        private DosingService service;
        private Portafilter currentPortafilter;
        private Coroutine autoDosingCoroutine;
        
        #region Properties
        
        public bool HasPortafilter => service?.HasPortafilter ?? false;
        public float StoredCoffeeAmount => service?.StoredCoffeeAmount ?? 0f;
        public float PortafilterCoffeeAmount => service?.PortafilterCoffeeAmount ?? 0f;
        public Portafilter CurrentPortafilter => currentPortafilter;
        public DosingService GetService() => service;
        
        #endregion
        
        #region Machine Lifecycle
        
        protected override void InitializeMachine()
        {
            InitializeService();
            ConfigureDropZones();
            SubscribeToEvents();
        }
        
        private void InitializeService()
        {
            service = new DosingService(config as DosingMachineConfig);
            ServiceManager.Instance?.RegisterMachineService<IDosingService>(service);
            
            Debug.Log($"DosingMachine: Service initialized");
        }
        
        private void ConfigureDropZones()
        {
            ConfigurePortafilterZone();
            ConfigureGroundCoffeeZone();
        }
        
        private void ConfigurePortafilterZone()
        {
            if (portafilterZone != null)
            {
                portafilterZone.AcceptPredicate = (item) => 
                {
                    return item is Portafilter && !service.HasPortafilter;
                };
            }
        }
        
        private void ConfigureGroundCoffeeZone()
        {
            if (groundCoffeeZone != null)
            {
                groundCoffeeZone.AcceptPredicate = (item) => 
                {
                    return item is GroundCoffee;
                };
            }
        }
        
        private void SubscribeToEvents()
        {
            if (service != null)
            {
                service.OnStateChanged += TransitionToState;
                service.OnProgressChanged += UpdateProgress;
                service.OnProcessCompleted += CompleteProcess;
                service.OnAutoDoseStarted += OnAutoDoseStarted;
                service.OnAutoDoseCompleted += OnAutoDoseCompleted;
            }
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromEvents();
            
            if (autoDosingCoroutine != null)
            {
                StopCoroutine(autoDosingCoroutine);
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (service != null)
            {
                service.OnStateChanged -= TransitionToState;
                service.OnProgressChanged -= UpdateProgress;
                service.OnProcessCompleted -= CompleteProcess;
                service.OnAutoDoseStarted -= OnAutoDoseStarted;
                service.OnAutoDoseCompleted -= OnAutoDoseCompleted;
            }
        }
        
        #endregion
        
        #region Unity Updates
        
        private void Update()
        {
            CheckPortafilterPresence();
        }
        
        private void LateUpdate()
        {
            // Check for auto-dosing at level 2
            if (upgradeLevel == 2 && service != null && service.ShouldAutoDose() && autoDosingCoroutine == null)
            {
                autoDosingCoroutine = StartCoroutine(AutoDosingProcess());
            }
        }
        
        private void CheckPortafilterPresence()
        {
            CheckPortafilterAdded();
            CheckPortafilterRemoved();
        }
        
        private void CheckPortafilterAdded()
        {
            if (portafilterZone == null || currentPortafilter != null) return;
            
            if (portafilterZone.transform.childCount > 0)
            {
                var portafilter = portafilterZone.transform.GetChild(0).GetComponent<Portafilter>();
                if (portafilter != null)
                {
                    currentPortafilter = portafilter;
                    
                    // Clear the Unity GameObject contents for fresh start
                    currentPortafilter.Clear();
                    
                    // Set service state (this also clears the service's portafilter amount)
                    service?.SetPortafilterPresent(true);
                    
                    Debug.Log("DosingMachine: New portafilter detected and cleared");
                }
            }
        }
        
        private void CheckPortafilterRemoved()
        {
            if (portafilterZone == null || currentPortafilter == null) return;
            
            if (portafilterZone.transform.childCount == 0)
            {
                currentPortafilter = null;
                service?.SetPortafilterPresent(false);
                Debug.Log("DosingMachine: Portafilter removed");
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Handle ground coffee being dropped into the machine
        /// </summary>
        public void OnGroundCoffeeDropped(Draggable item)
        {
            if (item is GroundCoffee groundCoffee)
            {
                float coffeeAmount = groundCoffee.GetAmount();
                bool added = service?.AddCoffee(coffeeAmount) ?? false;
                
                if (added)
                {
                    Destroy(groundCoffee.gameObject, 0.5f);
                    Debug.Log($"DosingMachine: Added {coffeeAmount}g coffee to storage");
                }
            }
        }
        
        /// <summary>
        /// Handle portafilter being dropped
        /// </summary>
        public void OnPortafilterDropped(Draggable item)
        {
            if (item is Portafilter portafilter)
            {
                currentPortafilter = portafilter;
                service?.SetPortafilterPresent(true);
                Debug.Log("DosingMachine: Portafilter placed");
            }
        }
        
        /// <summary>
        /// Handle portafilter being removed
        /// </summary>
        public void OnPortafilterRemoved(Draggable item)
        {
            if (item is Portafilter)
            {
                currentPortafilter = null;
                service?.SetPortafilterPresent(false);
                Debug.Log("DosingMachine: Portafilter removed");
            }
        }
        
        /// <summary>
        /// Handle manual dosing button hold (Level 0)
        /// </summary>
        public void OnDosingButtonHold(float duration)
        {
            if (service != null && upgradeLevel == 0)
            {
                float previousAmount = service.PortafilterCoffeeAmount;
                service.OnDispensingHold(Time.deltaTime);
                float newAmount = service.PortafilterCoffeeAmount;
                
                // Update Unity portafilter object
                if (currentPortafilter != null && newAmount != previousAmount)
                {
                    UpdatePortafilterContents(newAmount);
                }
            }
        }
        
        /// <summary>
        /// Handle manual dosing button release (Level 0)
        /// </summary>
        public void OnDosingButtonRelease(float heldDuration)
        {
            if (service != null && upgradeLevel == 0)
            {
                service.OnDispensingRelease();
            }
        }
        
        /// <summary>
        /// Handle auto-dose button click (Level 1)
        /// </summary>
        public void OnAutoDoseButtonClicked()
        {
            if (service != null && upgradeLevel == 1)
            {
                Debug.Log($"DosingMachine: Auto-dose button clicked - Current state: {service.CurrentState}");
                
                // Ensure we're not already processing
                if (service.CurrentState == MachineState.Processing)
                {
                    Debug.Log("DosingMachine: Already processing, ignoring button click");
                    return;
                }
                
                StartCoroutine(ButtonDosingProcess());
            }
        }
        
        #endregion
        
        #region Processing Coroutines
        
        /// <summary>
        /// Handle button-based dosing process (Level 1)
        /// </summary>
        private IEnumerator ButtonDosingProcess()
        {
            Debug.Log($"DosingMachine: Starting button dosing process - State: {service?.CurrentState}");
            
            if (!service.StartButtonProcess())
            {
                Debug.Log("DosingMachine: Failed to start button process");
                yield break;
            }
            
            float processingTime = service.GetProcessingTime();
            Debug.Log($"DosingMachine: Processing time: {processingTime}s");
            
            float startTime = Time.time;
            
            // Animate processing
            while (Time.time < startTime + processingTime)
            {
                float progress = (Time.time - startTime) / processingTime;
                UpdateProgress(progress);
                yield return null;
            }
            
            Debug.Log($"DosingMachine: Processing animation complete after {Time.time - startTime:F1}s");
            
            // Complete the process
            service.CompleteButtonProcess();
            
            // Update portafilter contents
            if (currentPortafilter != null)
            {
                UpdatePortafilterContents(service.PortafilterCoffeeAmount);
                Debug.Log($"DosingMachine: Updated portafilter to {service.PortafilterCoffeeAmount}g");
            }
        }
        
        /// <summary>
        /// Handle automatic dosing process (Level 2)
        /// </summary>
        private IEnumerator AutoDosingProcess()
        {
            Debug.Log("DosingMachine: Starting auto-dosing process");
            
            // Small delay for detection
            yield return new WaitForSeconds(0.3f);
            
            // Double-check conditions
            if (!service.ShouldAutoDose())
            {
                autoDosingCoroutine = null;
                yield break;
            }
            
            // Start processing
            service.ForceStateTransition(MachineState.Processing);
            
            float processingTime = service.GetProcessingTime();
            float startTime = Time.time;
            
            // Animate processing
            while (Time.time < startTime + processingTime)
            {
                float progress = (Time.time - startTime) / processingTime;
                UpdateProgress(progress);
                yield return null;
            }
            
            // Perform the actual dosing
            var calculation = service.PerformAutoDose();
            
            // Update portafilter contents
            if (currentPortafilter != null)
            {
                UpdatePortafilterContents(calculation.ResultingAmount);
            }
            
            // Auto-dose completed event will be fired by service
            autoDosingCoroutine = null;
            
            Debug.Log($"DosingMachine: Auto-dosing completed - {calculation.ResultingAmount:F1}g");
        }
        
        #endregion
        
        #region Unity Object Updates
        
        /// <summary>
        /// Update the actual portafilter GameObject contents
        /// </summary>
        private void UpdatePortafilterContents(float newAmount)
        {
            if (currentPortafilter != null)
            {
                currentPortafilter.Clear();
                if (newAmount > 0)
                {
                    currentPortafilter.TryAddItem("ground_coffee", newAmount);
                }
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnAutoDoseStarted()
        {
            Debug.Log("DosingMachine: Auto-dose started");
        }
        
        private void OnAutoDoseCompleted()
        {
            Debug.Log("DosingMachine: Auto-dose completed");
        }
        
        #endregion
        
        #region MachineBase Overrides
        
        public override void StartProcess()
        {
            base.StartProcess();
            
            if (service == null) return;
            
            var interactionType = service.GetInteractionType();
            
            switch (interactionType)
            {
                case InteractionType.ButtonPress:
                    StartCoroutine(ButtonDosingProcess());
                    break;
                case InteractionType.AutoProcess:
                    if (autoDosingCoroutine == null)
                    {
                        autoDosingCoroutine = StartCoroutine(AutoDosingProcess());
                    }
                    break;
                default:
                    // Manual lever handled through button events
                    break;
            }
        }
        
        protected override void ApplyUpgrade(int previousLevel, int newLevel)
        {
            service?.SetUpgradeLevel(newLevel);
            
            string message = newLevel switch
            {
                1 => "Upgraded to Semi-Auto dosing - Press button for perfect dose",
                2 => "Upgraded to Fully Automatic dosing - Auto-doses when portafilter is placed",
                _ => $"Dosing machine upgraded to level {newLevel}!"
            };
            
            NotifyUser(message);
            Debug.Log($"DosingMachine: {message}");
        }
        
        #endregion
    }
}
