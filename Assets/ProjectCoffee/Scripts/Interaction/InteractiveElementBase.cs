using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Base class for all interactive UI elements that provides common functionality
/// </summary>
public abstract class InteractiveElementBase : MonoBehaviour, IInteractiveElement
{
    [SerializeField] protected bool isActive = true;
    [SerializeField] protected float interactionCooldown = 0.2f;
    [SerializeField] protected AudioSource interactionSound;
    
    protected float lastInteractionTime;

    /// <summary>
    /// Custom predicate for determining if interaction is allowed
    /// </summary>
    public Func<bool> CanInteractCustomCheck { get; set; }
    
    /// <summary>
    /// Check if the element can be interacted with
    /// </summary>
    public virtual bool CanInteract()
    {
        // Base check includes:
        // 1. Element is active
        // 2. Not in cooldown
        // 3. Custom check passes (if provided)
        bool outOfCooldown = Time.time - lastInteractionTime >= interactionCooldown;
        bool customCheck = CanInteractCustomCheck?.Invoke() ?? true;
        
        return isActive && outOfCooldown && customCheck;
    }
    
    /// <summary>
    /// Called when interaction with the element starts
    /// </summary>
    public virtual void OnInteractionStart()
    {
        lastInteractionTime = Time.time;
        
        // Play interaction sound
        InteractionFeedbackHelper.PlaySound(interactionSound);
        
        // Visual feedback
        InteractionFeedbackHelper.PlayStartInteractionAnimation(transform);
    }
    
    /// <summary>
    /// Called when interaction with the element ends
    /// </summary>
    public virtual void OnInteractionEnd()
    {
        // Visual feedback
        InteractionFeedbackHelper.PlayEndInteractionAnimation(transform);
    }
    
    /// <summary>
    /// Set whether the element is active/interactable
    /// </summary>
    public virtual void SetActive(bool active)
    {
        isActive = active;
    }
}