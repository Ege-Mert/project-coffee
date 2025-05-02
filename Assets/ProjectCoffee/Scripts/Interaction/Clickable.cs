using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Base class for clickable UI elements
/// </summary>
public class Clickable : InteractiveElementBase, IPointerClickHandler
{
    [SerializeField] protected AudioSource clickSound;
    
    /// <summary>
    /// Called when the UI element is clicked
    /// </summary>
    public virtual void OnPointerClick(PointerEventData eventData)
    {
        if (!CanInteract())
            return;
            
        // Update last interaction time
        lastInteractionTime = Time.time;
        
        // Play click sound
        InteractionFeedbackHelper.PlaySound(clickSound);
        
        // Visual feedback
        InteractionFeedbackHelper.PlayClickAnimation(transform);
        
        OnInteractionStart();
        OnClick();
        OnInteractionEnd();
    }
    
    /// <summary>
    /// Called when the button is clicked and interaction is allowed
    /// </summary>
    protected virtual void OnClick()
    {
        // Override in derived classes
    }
}