using System;

namespace ProjectCoffee.Machines.Grinder.Logic
{
    /// <summary>
    /// Pure business logic for coffee grinding operations.
    /// Contains no Unity dependencies and can be easily unit tested.
    /// </summary>
    public class GrinderLogic
    {
        private readonly GrinderConfig config;
        
        public GrinderLogic(GrinderConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
        }
        
        #region Bean Management
        
        /// <summary>
        /// Check if beans can be added to the grinder
        /// </summary>
        public bool CanAddBeans(int currentBeans, int beansToAdd)
        {
            if (beansToAdd <= 0) return false;
            return currentBeans + beansToAdd <= config.maxBeanFills;
        }
        
        /// <summary>
        /// Calculate how many beans can actually be added
        /// </summary>
        public int GetMaxAddableBeans(int currentBeans)
        {
            return Math.Max(0, config.maxBeanFills - currentBeans);
        }
        
        /// <summary>
        /// Validate bean addition and return the actual amount that can be added
        /// </summary>
        public GrinderOperationResult<int> AddBeans(int currentBeans, int beansToAdd)
        {
            if (beansToAdd <= 0)
                return GrinderOperationResult<int>.Failure("Cannot add zero or negative beans");
            
            if (currentBeans >= config.maxBeanFills)
                return GrinderOperationResult<int>.Failure("Grinder is already full");
            
            int actualBeansAdded = Math.Min(beansToAdd, config.maxBeanFills - currentBeans);
            int newBeanCount = currentBeans + actualBeansAdded;
            
            return GrinderOperationResult<int>.Success(newBeanCount, 
                $"Added {actualBeansAdded} beans. Total: {newBeanCount}/{config.maxBeanFills}");
        }
        
        #endregion
        
        #region Grinding Logic
        
        /// <summary>
        /// Check if grinding can be performed
        /// </summary>
        public bool CanGrind(int currentBeans, bool hasExistingCoffee, GroundCoffee.GrindSize currentSize)
        {
            if (currentBeans <= 0) return false;
            
            // If there's existing coffee at max size, can't grind more
            if (hasExistingCoffee && currentSize == GroundCoffee.GrindSize.Large)
                return false;
                
            return true;
        }
        
        /// <summary>
        /// Process grinding operation and return the result
        /// </summary>
        public GrinderOperationResult<GrindingResult> ProcessGrinding(
            int currentBeans, 
            bool hasExistingCoffee, 
            GroundCoffee.GrindSize currentSize)
        {
            if (!CanGrind(currentBeans, hasExistingCoffee, currentSize))
            {
                return GrinderOperationResult<GrindingResult>.Failure("Cannot grind: insufficient beans or coffee at max size");
            }
            
            // Consume one bean fill
            int newBeanCount = currentBeans - 1;
            
            if (!hasExistingCoffee)
            {
                // Create new coffee at small size
                var result = new GrindingResult
                {
                    NewBeanCount = newBeanCount,
                    ResultSize = GroundCoffee.GrindSize.Small,
                    IsNewCoffee = true,
                    CoffeeAmount = GetAmountForSize(GroundCoffee.GrindSize.Small)
                };
                
                return GrinderOperationResult<GrindingResult>.Success(result, 
                    "Created new ground coffee (Small size)");
            }
            else
            {
                // Upgrade existing coffee
                GroundCoffee.GrindSize newSize = GetNextSize(currentSize);
                
                var result = new GrindingResult
                {
                    NewBeanCount = newBeanCount,
                    ResultSize = newSize,
                    IsNewCoffee = false,
                    CoffeeAmount = GetAmountForSize(newSize)
                };
                
                return GrinderOperationResult<GrindingResult>.Success(result, 
                    $"Upgraded coffee to {newSize} size");
            }
        }
        
        /// <summary>
        /// Get the next grind size for upgrading
        /// </summary>
        public GroundCoffee.GrindSize GetNextSize(GroundCoffee.GrindSize currentSize)
        {
            return currentSize switch
            {
                GroundCoffee.GrindSize.Small => GroundCoffee.GrindSize.Medium,
                GroundCoffee.GrindSize.Medium => GroundCoffee.GrindSize.Large,
                GroundCoffee.GrindSize.Large => GroundCoffee.GrindSize.Large, // Max size
                _ => GroundCoffee.GrindSize.Small
            };
        }
        
        /// <summary>
        /// Get the amount of coffee for a specific grind size
        /// </summary>
        public float GetAmountForSize(GroundCoffee.GrindSize size)
        {
            int sizeIndex = (int)size;
            if (config.groundCoffeeSizes != null && sizeIndex < config.groundCoffeeSizes.Length)
            {
                return config.groundCoffeeSizes[sizeIndex];
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
        
        #region Upgrade Logic
        
        /// <summary>
        /// Get the required number of spins for the current upgrade level
        /// </summary>
        public int GetRequiredSpins(int upgradeLevel)
        {
            return upgradeLevel switch
            {
                0 => config.level0SpinsRequired,
                _ => 1 // For levels 1+ it's always 1 spin/button press
            };
        }
        
        /// <summary>
        /// Get the process time for the current upgrade level
        /// </summary>
        public float GetProcessTime(int upgradeLevel)
        {
            return upgradeLevel switch
            {
                0 => 0f, // Manual operation, no time
                1 => config.level1GrindTime,
                2 => config.level2GrindTime,
                _ => config.level1GrindTime
            };
        }
        
        /// <summary>
        /// Get the auto-process delay for level 2
        /// </summary>
        public float GetAutoProcessDelay()
        {
            return config.level2AutoProcessDelay;
        }
        
        /// <summary>
        /// Get the process delay for the specified upgrade level
        /// </summary>
        public float GetProcessDelay(int upgradeLevel)
        {
            return upgradeLevel switch
            {
                1 => config.level1ProcessDelay,
                2 => config.level2AutoProcessDelay,
                _ => 0f
            };
        }
        
        /// <summary>
        /// Get the interaction type for the current upgrade level
        /// </summary>
        public InteractionType GetInteractionType(int upgradeLevel)
        {
            return upgradeLevel switch
            {
                0 => InteractionType.ManualLever,
                1 => InteractionType.ButtonPress,
                2 => InteractionType.AutoProcess,
                _ => InteractionType.ManualLever
            };
        }
        
        /// <summary>
        /// Check if auto-processing should occur
        /// </summary>
        public bool ShouldAutoProcess(int upgradeLevel, int currentBeans, bool hasExistingCoffee, GroundCoffee.GrindSize currentSize)
        {
            if (upgradeLevel < 2) return false;
            if (!CanGrind(currentBeans, hasExistingCoffee, currentSize)) return false;
            
            // Auto-process if we can grind and either:
            // 1. No existing coffee, OR
            // 2. Coffee exists but isn't at max size
            return !hasExistingCoffee || currentSize != GroundCoffee.GrindSize.Large;
        }
        
        #endregion
    }
    
    #region Data Structures
    
    /// <summary>
    /// Result of a grinding operation
    /// </summary>
    public class GrindingResult
    {
        public int NewBeanCount { get; set; }
        public GroundCoffee.GrindSize ResultSize { get; set; }
        public bool IsNewCoffee { get; set; }
        public float CoffeeAmount { get; set; }
    }
    
    /// <summary>
    /// Generic operation result with success/failure state and message
    /// </summary>
    public class GrinderOperationResult<T>
    {
        public bool IsSuccess { get; private set; }
        public T Data { get; private set; }
        public string Message { get; private set; }
        
        private GrinderOperationResult(bool isSuccess, T data, string message)
        {
            IsSuccess = isSuccess;
            Data = data;
            Message = message;
        }
        
        public static GrinderOperationResult<T> Success(T data, string message = "")
        {
            return new GrinderOperationResult<T>(true, data, message);
        }
        
        public static GrinderOperationResult<T> Failure(string message)
        {
            return new GrinderOperationResult<T>(false, default(T), message);
        }
    }
    
    #endregion
}
