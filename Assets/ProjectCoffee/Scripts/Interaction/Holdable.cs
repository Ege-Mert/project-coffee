using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Base class for holdable UI elements
/// </summary>
public class Holdable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IInteractiveElement
{
    [SerializeField] protected bool isActive = true;
    [SerializeField] protected float maxHoldTime = 5f;
    [SerializeField] protected Image fillIndicator;
    [SerializeField] protected AudioSource startHoldSound;
    [SerializeField] protected AudioSource endHoldSound;
    [SerializeField] protected AudioSource holdingSound;
    
    protected bool isHolding = false;
    protected float holdStartTime;
    protected float currentHoldDuration;
    
    public delegate void HoldHandler(float duration);
    public delegate void HoldReleaseHandler(float duration);
    
    /// <summary>
    /// Called every frame while holding, with the current hold duration
    /// </summary>
    public HoldHandler OnHold;
    
    /// <summary>
    /// Called when hold is released, with the final hold duration
    /// </summary>
    public HoldReleaseHandler OnHoldRelease;
    
    /// <summary>
    /// Custom function to check if interaction is allowed
    /// </summary>
    public Func<bool> CanInteract = () => true;
    
    protected virtual void Update()
    {
        if (isHolding)
        {
            currentHoldDuration = Time.time - holdStartTime;
            float holdProgress = Mathf.Clamp01(currentHoldDuration / maxHoldTime);
            
            // Update visual indicator if available
            if (fillIndicator != null)
            {
                fillIndicator.fillAmount = holdProgress;
            }
            
            // Call the hold callback
            OnHold?.Invoke(currentHoldDuration);
            
            if (currentHoldDuration >= maxHoldTime)
            {
                // Max hold time reached
                isHolding = false;
                OnHoldComplete();
            }
        }
    }
    
    bool IInteractiveElement.CanInteract()
    {
        return isActive && CanInteract();
    }
    
    public virtual void OnInteractionStart()
    {
        // Visual feedback when interaction starts
        if (transform != null)
        {
            InteractionFeedbackHelper.PlayClickAnimation(transform);
        }
    }
    
    public virtual void OnInteractionEnd()
    {
        // Visual feedback when interaction ends
        if (transform != null)
        {
            transform.localScale = Vector3.one; // Ensure scale is reset
        }
    }
    
    public virtual void OnPointerDown(PointerEventData eventData)
    {
        // IMPORTANT: Using the explicit interface check instead of our custom function
        if (!((IInteractiveElement)this).CanInteract())
        {
            Debug.Log($"Hold prevented on {gameObject.name}: CanInteract returned false");
            return;
        }
        
        Debug.Log($"OnPointerDown on {gameObject.name}, isActive: {isActive}, CustomCheck: {CanInteract()}");
        
        isHolding = true;
        holdStartTime = Time.time;
        currentHoldDuration = 0f;
        
        if (fillIndicator != null)
        {
            fillIndicator.fillAmount = 0f;
            fillIndicator.gameObject.SetActive(true);
        }
        
        // Play start sound
        if (startHoldSound != null && startHoldSound.isActiveAndEnabled)
        {
            startHoldSound.Play();
        }
        
        // Play looping sound
        if (holdingSound != null && holdingSound.isActiveAndEnabled && !holdingSound.isPlaying)
        {
            holdingSound.Play();
        }
        
        OnInteractionStart();
    }
    
    public virtual void OnPointerUp(PointerEventData eventData)
    {
        if (!isHolding)
            return;
        
        Debug.Log($"OnPointerUp on {gameObject.name}, held for {currentHoldDuration}s");
        
        isHolding = false;
        
        if (fillIndicator != null)
        {
            fillIndicator.gameObject.SetActive(false);
        }
        
        // Stop looping sound
        if (holdingSound != null && holdingSound.isActiveAndEnabled && holdingSound.isPlaying)
        {
            holdingSound.Stop();
        }
        
        // Play end sound
        if (endHoldSound != null && endHoldSound.isActiveAndEnabled)
        {
            endHoldSound.Play();
        }
        
        // Pass the current hold duration to the callback
        OnHoldRelease?.Invoke(currentHoldDuration);
        
        OnInteractionEnd();
    }
    
    protected virtual void OnHoldComplete()
    {
        Debug.Log($"Hold completed on {gameObject.name}");
        
        // Stop looping sound
        if (holdingSound != null && holdingSound.isActiveAndEnabled && holdingSound.isPlaying)
        {
            holdingSound.Stop();
        }
        
        // Play end sound
        if (endHoldSound != null && endHoldSound.isActiveAndEnabled)
        {
            endHoldSound.Play();
        }
        
        OnInteractionEnd();
    }
}