using System;
using UnityEngine;
using ProjectCoffee.Services.Interfaces;

namespace ProjectCoffee.Services
{
    public class CoffeeGrinderService : MachineService, IGrinderService
    {
        public event Action<int> OnBeanCountChanged;
        public event Action<GroundCoffee.GrindSize> OnCoffeeOutputReady;
        public event Action<int> OnSpinCompleted;
        public event Action<GroundCoffee.GrindSize> OnCoffeeSizeUpgraded;
        public event Action OnGrindingStarted;
        
        private int currentBeanFills = 0;
        private int spinsSinceLastBeanConsumption = 0;
        private bool isProcessingGrinding = false;
        private bool hasExistingGroundCoffee = false;
        private GroundCoffee.GrindSize currentCoffeeSize = GroundCoffee.GrindSize.Small;
        
        public int CurrentBeanFills => currentBeanFills;
        public int MaxBeanFills => config != null ? ((GrinderConfig)config).maxBeanFills : 3;
        public bool CanAddBeans => currentBeanFills < MaxBeanFills;
        public bool HasBeans => currentBeanFills > 0;
        public bool HasExistingGroundCoffee => hasExistingGroundCoffee;
        public GroundCoffee.GrindSize CurrentCoffeeSize => currentCoffeeSize;
        
        public CoffeeGrinderService(GrinderConfig config) : base(config) { }
        
        public void OnGroundCoffeeCreated()
        {
            if (currentBeanFills > 0)
            {
                currentBeanFills--;
                OnBeanCountChanged?.Invoke(currentBeanFills);
            }
            
            hasExistingGroundCoffee = true;
            currentCoffeeSize = GroundCoffee.GrindSize.Small;
            UpdateState();
        }
        
        public void OnGroundCoffeeSizeChanged(GroundCoffee.GrindSize newSize)
        {
            currentCoffeeSize = newSize;
            hasExistingGroundCoffee = true;
            
            if (currentBeanFills > 0)
            {
                currentBeanFills--;
                OnBeanCountChanged?.Invoke(currentBeanFills);
            }
            UpdateState();
        }
        
        public bool AddBeans(int amount)
        {
            if (!CanAddBeans)
            {
                NotifyUser("Grinder is full of beans!");
                return false;
            }
            
            int previousAmount = currentBeanFills;
            currentBeanFills = Mathf.Min(currentBeanFills + amount, MaxBeanFills);
            
            if (currentBeanFills != previousAmount)
            {
                OnBeanCountChanged?.Invoke(currentBeanFills);
                NotifyUser($"Added beans. Total: {currentBeanFills}/{MaxBeanFills}");
                UpdateState();
                Debug.Log($"GrinderService: Beans added. Count: {currentBeanFills}, State: {currentState}");
            }
            
            return true;
        }
        
        public void OnHandleSpinCompleted()
        {
            Debug.Log($"GrinderService: Handle spin completed. HasBeans: {HasBeans}, IsProcessing: {isProcessingGrinding}, State: {currentState}");
            
            if (!HasBeans || isProcessingGrinding) 
            {
                Debug.Log("GrinderService: Cannot process spin - no beans or already processing");
                return;
            }
            
            // Fixed: Ensure we're in ready state before processing
            if (currentState == MachineState.Idle && HasBeans)
            {
                Debug.Log("GrinderService: Transitioning from Idle to Ready");
                TransitionTo(MachineState.Ready);
            }
            
            spinsSinceLastBeanConsumption++;
            OnSpinCompleted?.Invoke(spinsSinceLastBeanConsumption);
            
            int requiredSpins = GetRequiredSpins();
            Debug.Log($"GrinderService: Spin {spinsSinceLastBeanConsumption}/{requiredSpins}");
            
            if (spinsSinceLastBeanConsumption >= requiredSpins)
            {
                Debug.Log("GrinderService: Required spins reached, processing grinding");
                ProcessGrinding();
            }
        }
        
        public void OnButtonPressed()
        {
            Debug.Log($"GrinderService: Button pressed. Processing: {isProcessingGrinding}, UpgradeLevel: {upgradeLevel}, HasBeans: {HasBeans}");
            
            if (isProcessingGrinding) return;
                
            if (upgradeLevel < 1 || !HasBeans) return;
            
            StartProcessing();
        }
        
        public override void StartProcessing()
        {
            if (isProcessingGrinding || !CanProcess()) 
            {
                Debug.Log($"GrinderService: Cannot start processing. IsProcessing: {isProcessingGrinding}, CanProcess: {CanProcess()}");
                return;
            }
            
            Debug.Log("GrinderService: Starting processing");
            base.StartProcessing();
            isProcessingGrinding = true;
            
            if (upgradeLevel >= 1)
            {
                float processTime = ((GrinderConfig)config).level1GrindTime;
                
                if (upgradeLevel >= 2)
                {
                    processTime = ((GrinderConfig)config).level2GrindTime;
                    processProgress = 0.5f;
                }
                
                NotifyUser("Grinding coffee...");
                Debug.Log($"GrinderService: Process time set to {processTime}s");
            }
        }
        
        public void ForceCompleteProcessing()
        {
            if (currentState != MachineState.Processing) return;
            
            Debug.Log("GrinderService: Force completing processing");
            processProgress = 1.0f;
            UpdateProgress(processProgress);
            ProcessGrinding();
        }
        
        public void ProcessUpdate(float deltaTime)
        {
            if (currentState != MachineState.Processing) return;
            
            float processTime = upgradeLevel >= 2 ? 
                ((GrinderConfig)config).level2GrindTime : 
                ((GrinderConfig)config).level1GrindTime;
                
            processProgress += deltaTime / processTime;
            UpdateProgress(processProgress);
            
            if (processProgress >= 1.0f)
            {
                Debug.Log("GrinderService: Processing complete via time update");
                isProcessingGrinding = false;
                ProcessGrinding();
            }
        }
        
        private void ProcessGrinding()
        {
            Debug.Log($"GrinderService: ProcessGrinding called. HasBeans: {HasBeans}, HasExistingCoffee: {hasExistingGroundCoffee}");
            
            if (!HasBeans)
            {
                Debug.Log("GrinderService: No beans available, completing processing");
                isProcessingGrinding = false;
                CompleteProcessing();
                UpdateState();
                return;
            }
            
            // Fixed: Set processing state properly
            if (currentState != MachineState.Processing)
            {
                TransitionTo(MachineState.Processing);
            }
            
            isProcessingGrinding = true;
            
            try
            {
                if (!hasExistingGroundCoffee)
                {
                    Debug.Log("GrinderService: Creating new ground coffee (Small)");
                    currentBeanFills--;
                    OnBeanCountChanged?.Invoke(currentBeanFills);
                    
                    currentCoffeeSize = GroundCoffee.GrindSize.Small;
                    hasExistingGroundCoffee = true;
                    OnCoffeeOutputReady?.Invoke(currentCoffeeSize);
                    spinsSinceLastBeanConsumption = 0;
                }
                else
                {
                    if (currentCoffeeSize == GroundCoffee.GrindSize.Small)
                    {
                        Debug.Log("GrinderService: Upgrading coffee to Medium");
                        currentBeanFills--;
                        OnBeanCountChanged?.Invoke(currentBeanFills);
                        
                        currentCoffeeSize = GroundCoffee.GrindSize.Medium;
                        OnCoffeeSizeUpgraded?.Invoke(currentCoffeeSize);
                        spinsSinceLastBeanConsumption = 0;
                    }
                    else if (currentCoffeeSize == GroundCoffee.GrindSize.Medium)
                    {
                        Debug.Log("GrinderService: Upgrading coffee to Large");
                        currentBeanFills--;
                        OnBeanCountChanged?.Invoke(currentBeanFills);
                        
                        currentCoffeeSize = GroundCoffee.GrindSize.Large;
                        OnCoffeeSizeUpgraded?.Invoke(currentCoffeeSize);
                        spinsSinceLastBeanConsumption = 0;
                    }
                    else
                    {
                        Debug.Log("GrinderService: Coffee already at maximum size");
                        spinsSinceLastBeanConsumption = 0;
                        NotifyUser("Ground coffee is already at maximum size!");
                    }
                }
            }
            finally
            {
                isProcessingGrinding = false;
                CompleteProcessing();
                UpdateState();
                Debug.Log($"GrinderService: Processing complete. New state: {currentState}");
            }
        }
        
        public void OnGroundCoffeeRemoved()
        {
            Debug.Log("GrinderService: Ground coffee removed");
            hasExistingGroundCoffee = false;
            currentCoffeeSize = GroundCoffee.GrindSize.Small;
            spinsSinceLastBeanConsumption = 0;
            UpdateState();
        }
        
        private int GetRequiredSpins()
        {
            return upgradeLevel == 0 ? 1 : 1;
        }
        
        private void UpdateState()
        {
            MachineState previousState = currentState;
            
            if (HasBeans && currentState == MachineState.Idle)
            {
                TransitionTo(MachineState.Ready);
                Debug.Log($"GrinderService: State updated from {previousState} to Ready (has beans)");
            }
            else if (!HasBeans && currentState != MachineState.Idle && currentState != MachineState.Processing)
            {
                TransitionTo(MachineState.Idle);
                Debug.Log($"GrinderService: State updated from {previousState} to Idle (no beans)");
            }
        }

        public void ForceReadyState()
        {
            if (currentState != MachineState.Ready && HasBeans)
            {
                Debug.Log($"GrinderService: Force transitioning from {currentState} to Ready");
                TransitionTo(MachineState.Ready);
            }
        }
        
        protected override void OnUpgradeLevelChanged(int level)
        {
            Debug.Log($"GrinderService: Upgrade level changed to {level}");
            
            switch (level)
            {
                case 0:
                    NotifyUser("Basic grinder: Use the lever to grind beans.");
                    break;
                case 1:
                    NotifyUser("Grinder upgraded to level 1! Now uses button press.");
                    break;
                case 2:
                    NotifyUser("Grinder upgraded to level 2! Now automatically grinds all beans.");
                    break;
            }
        }
        
        public void CheckAutoProcess()
        {
            if (upgradeLevel >= 2 && HasBeans && !isProcessingGrinding && currentState == MachineState.Ready)
            {
                if (!hasExistingGroundCoffee || currentCoffeeSize != GroundCoffee.GrindSize.Large)
                {
                    Debug.Log("GrinderService: Auto-processing triggered");
                    StartProcessing();
                }
            }
        }
    }
}