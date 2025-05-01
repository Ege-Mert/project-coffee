using UnityEngine;

public class BrewButton : Clickable
{
    [SerializeField] private EspressoMachine espressoMachine;
    
    protected override void OnClick()
    {
        if (espressoMachine != null)
        {
            espressoMachine.OnBrewButtonClick();
        }
        else
        {
            Debug.LogError("BrewButton: No EspressoMachineUI reference set!");
        }
    }
}