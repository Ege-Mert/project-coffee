using System;
using System.Collections;
using DG.Tweening;
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
    // [SerializeField] protected AudioSource startHoldSound;
    // [SerializeField] protected AudioSource endHoldSound;
    // [SerializeField] protected AudioSource holdingSound;
    
    protected bool isHolding = false;
    protected float holdStartTime;
    protected float currentHoldDuration;
    
    public delegate void HoldHandler(float duration);
    public delegate void HoldReleaseHandler(float duration);
    
    public HoldHandler OnHold;
    public HoldReleaseHandler OnHoldRelease;
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
        transform.DOScale(0.9f, 0.1f);
    }
    
    public virtual void OnInteractionEnd()
    {
        // Visual feedback when interaction ends
        transform.DOScale(1.0f, 0.1f);
    }
    
    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if (!((IInteractiveElement)this).CanInteract())
            return;
            
        isHolding = true;
        holdStartTime = Time.time;
        currentHoldDuration = 0f;
        
        if (fillIndicator != null)
        {
            fillIndicator.fillAmount = 0f;
            fillIndicator.gameObject.SetActive(true);
        }
        
        // if (startHoldSound != null)
        // {
        //     startHoldSound.Play();
        // }
        
        // if (holdingSound != null)
        // {
        //     holdingSound.Play();
        // }
        
        OnInteractionStart();
    }
    
    public virtual void OnPointerUp(PointerEventData eventData)
    {
        if (!isHolding)
            return;
            
        isHolding = false;
        
        if (fillIndicator != null)
        {
            fillIndicator.gameObject.SetActive(false);
        }
        
        // if (holdingSound != null && holdingSound.isPlaying)
        // {
        //     holdingSound.Stop();
        // }
        
        // if (endHoldSound != null)
        // {
        //     endHoldSound.Play();
        // }
        
        OnHoldRelease?.Invoke(currentHoldDuration);
        OnInteractionEnd();
    }
    
    protected virtual void OnHoldComplete()
    {
        // Override in derived classes to handle completed hold
        // if (holdingSound != null && holdingSound.isPlaying)
        // {
        //     holdingSound.Stop();
        // }
        
        // if (endHoldSound != null)
        // {
        //     endHoldSound.Play();
        // }
        
        OnInteractionEnd();
    }
}