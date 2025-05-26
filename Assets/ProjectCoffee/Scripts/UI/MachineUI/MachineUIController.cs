using UnityEngine;
using ProjectCoffee.Machines;
using ProjectCoffee.Services;

namespace ProjectCoffee.UI.MachineUI
{
    public abstract class MachineUIController<TMachine, TService, TConfig> : MonoBehaviour 
        where TMachine : Machine<TService, TConfig>
        where TService : MachineService 
        where TConfig : MachineConfig
    {
        [Header("References")]
        [SerializeField] protected TMachine machine;
        
        [Header("Visual State Indicators")]
        [SerializeField] protected GameObject idleIndicator;
        [SerializeField] protected GameObject readyIndicator;
        [SerializeField] protected GameObject processingIndicator;
        [SerializeField] protected GameObject completeIndicator;
        
        [Header("Effects")]
        [SerializeField] protected AudioSource processStartSound;
        [SerializeField] protected AudioSource processCompleteSound;
        [SerializeField] protected ParticleSystem processingParticles;
        [SerializeField] protected Animator machineAnimator;
        
        protected TService Service => machine?.GetService();
        
        protected virtual void Start()
        {
            if (machine == null)
                machine = GetComponent<TMachine>();
                
            if (machine != null)
            {
                machine.OnStateChanged.AddListener(HandleStateChanged);
                machine.OnProgressUpdated.AddListener(HandleProgressUpdated);
                machine.OnProcessingCompleted.AddListener(HandleProcessingCompleted);
            }
            
            UpdateVisualState(MachineState.Idle);
        }
        
        protected virtual void OnDestroy()
        {
            if (machine != null)
            {
                machine.OnStateChanged.RemoveListener(HandleStateChanged);
                machine.OnProgressUpdated.RemoveListener(HandleProgressUpdated);
                machine.OnProcessingCompleted.RemoveListener(HandleProcessingCompleted);
            }
        }
        
        protected virtual void HandleStateChanged(MachineState newState)
        {
            UpdateVisualState(newState);
            
            if (machineAnimator != null)
                machineAnimator.SetTrigger($"To{newState}");
        }
        
        protected virtual void HandleProgressUpdated(float progress)
        {
            // Override in derived classes for progress visualization
        }
        
        protected virtual void HandleProcessingCompleted()
        {
            if (processCompleteSound != null)
                processCompleteSound.Play();
                
            if (processingParticles != null)
                processingParticles.Stop();
        }
        
        protected virtual void UpdateVisualState(MachineState state)
        {
            if (idleIndicator != null) idleIndicator.SetActive(state == MachineState.Idle);
            if (readyIndicator != null) readyIndicator.SetActive(state == MachineState.Ready);
            if (processingIndicator != null) processingIndicator.SetActive(state == MachineState.Processing);
            if (completeIndicator != null) completeIndicator.SetActive(state == MachineState.Complete);
            
            if (state == MachineState.Processing)
            {
                if (processStartSound != null) processStartSound.Play();
                if (processingParticles != null) processingParticles.Play();
            }
        }
    }
}