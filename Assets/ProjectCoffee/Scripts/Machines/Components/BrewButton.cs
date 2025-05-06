using UnityEngine;
using UnityEngine.Events;

public class BrewButton : Clickable
{
    // Optional: reference can be set in inspector, but we'll use events instead
    [SerializeField] private EspressoMachine espressoMachine;
    
    // Event for when button is clicked
    public UnityEvent OnClicked;
    
    protected override void OnClick()
    {
        // If we have a direct reference, use it (for backward compatibility)
        if (espressoMachine != null)
        {
            espressoMachine.OnBrewButtonClick();
        }
        
        // Invoke the Unity event so other components can respond
        OnClicked?.Invoke();
    }
}