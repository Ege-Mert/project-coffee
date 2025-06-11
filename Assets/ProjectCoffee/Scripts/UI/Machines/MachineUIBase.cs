using UnityEngine;
using UnityEngine.UI;
using ProjectCoffee.Services;
using ProjectCoffee.Machines;

namespace ProjectCoffee.UI.Machines
{
    /// <summary>
    /// Base class for all machine UI controllers
    /// </summary>
    public abstract class MachineUIBase<TMachine> : MonoBehaviour 
        where TMachine : MonoBehaviour
    {
        [Header("Base UI References")]
        [SerializeField] protected GameObject idleIndicator;
        [SerializeField] protected GameObject readyIndicator;
        [SerializeField] protected GameObject processingIndicator;
        [SerializeField] protected GameObject completeIndicator;
        [SerializeField] protected Image progressBar;
        [SerializeField] protected Text statusText;
        
        [Header("Effects")]
        [SerializeField] protected ParticleSystem processingParticles;
        [SerializeField] protected AudioSource processStartSound;
        [SerializeField] protected AudioSource processCompleteSound;
        [SerializeField] protected Animator machineAnimator;
        
        protected TMachine Machine { get; private set; }
        
        protected virtual void Awake()
        {
            Machine = GetComponent<TMachine>();
            if (Machine == null)
            {
                Debug.LogError($"Machine component of type {typeof(TMachine).Name} not found on {gameObject.name}");
                enabled = false;
                return;
            }
        }
        
        protected virtual void Start()
        {
            // Delay UI setup to ensure machine is initialized first
            StartCoroutine(DelayedUISetup());
        }
        
        protected virtual System.Collections.IEnumerator DelayedUISetup()
        {
            // Wait for machine to initialize
            yield return null;
            
            SetupMachineSpecificUI();
            SubscribeToEvents();
            UpdateVisualState(MachineState.Idle);
        }
        
        /// <summary>
        /// Setup machine-specific UI elements
        /// </summary>
        protected abstract void SetupMachineSpecificUI();
        
        /// <summary>
        /// Subscribe to machine events
        /// </summary>
        protected virtual void SubscribeToEvents()
        {
            // Subscribe to base machine events if available
            var machineBase = Machine as IMachineEvents;
            if (machineBase != null)
            {
                machineBase.OnStateChanged += HandleStateChanged;
                machineBase.OnProgressChanged += HandleProgressChanged;
                machineBase.OnProcessCompleted += HandleProcessCompleted;
                machineBase.OnUpgradeApplied += HandleUpgradeApplied;
            }
        }
        
        /// <summary>
        /// Handle state change from machine
        /// </summary>
        protected virtual void HandleStateChanged(MachineState newState)
        {
            UpdateVisualState(newState);
            
            // Play appropriate animations
            if (machineAnimator != null)
            {
                machineAnimator.SetTrigger($"To{newState}");
            }
            
            // Handle particles and sounds
            if (newState == MachineState.Processing)
            {
                if (processingParticles != null)
                    processingParticles.Play();
                    
                if (processStartSound != null)
                    processStartSound.Play();
            }
            else
            {
                if (processingParticles != null)
                    processingParticles.Stop();
            }
        }
        
        /// <summary>
        /// Handle progress updates from machine
        /// </summary>
        protected virtual void HandleProgressChanged(float progress)
        {
            if (progressBar != null)
            {
                progressBar.fillAmount = progress;
            }
        }
        
        /// <summary>
        /// Handle process completion from machine
        /// </summary>
        protected virtual void HandleProcessCompleted()
        {
            if (processCompleteSound != null)
            {
                processCompleteSound.Play();
            }
            
            if (processingParticles != null)
            {
                processingParticles.Stop();
            }
        }
        
        /// <summary>
        /// Handle upgrade level changes
        /// </summary>
        protected virtual void HandleUpgradeApplied(int level)
        {
            Debug.Log($"{GetType().Name} handling upgrade to level {level}");
        }
        
        /// <summary>
        /// Update visual indicators based on state
        /// </summary>
        protected virtual void UpdateVisualState(MachineState state)
        {
            if (idleIndicator != null) idleIndicator.SetActive(state == MachineState.Idle);
            if (readyIndicator != null) readyIndicator.SetActive(state == MachineState.Ready);
            if (processingIndicator != null) processingIndicator.SetActive(state == MachineState.Processing);
            if (completeIndicator != null) completeIndicator.SetActive(state == MachineState.Complete);
            
            if (statusText != null)
            {
                statusText.text = GetStatusText(state);
            }
        }
        
        /// <summary>
        /// Get status text for a given state
        /// </summary>
        protected virtual string GetStatusText(MachineState state)
        {
            switch (state)
            {
                case MachineState.Idle:
                    return "Idle";
                case MachineState.Ready:
                    return "Ready";
                case MachineState.Processing:
                    return "Processing...";
                case MachineState.Complete:
                    return "Complete!";
                case MachineState.Error:
                    return "Error";
                default:
                    return state.ToString();
            }
        }
        
        protected virtual void OnDestroy()
        {
            // Unsubscribe from events
            var machineBase = Machine as IMachineEvents;
            if (machineBase != null)
            {
                machineBase.OnStateChanged -= HandleStateChanged;
                machineBase.OnProgressChanged -= HandleProgressChanged;
                machineBase.OnProcessCompleted -= HandleProcessCompleted;
                machineBase.OnUpgradeApplied -= HandleUpgradeApplied;
            }
        }
    }
    
    /// <summary>
    /// Interface for machine events that UI can subscribe to
    /// </summary>
    public interface IMachineEvents
    {
        event System.Action<MachineState> OnStateChanged;
        event System.Action<float> OnProgressChanged;
        event System.Action OnProcessCompleted;
        event System.Action<int> OnUpgradeApplied;
    }
}
