using UnityEngine;

public class BrewButton : ClickableUI
{
    [SerializeField] private EspressoMachineUI espressoMachine;
    
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