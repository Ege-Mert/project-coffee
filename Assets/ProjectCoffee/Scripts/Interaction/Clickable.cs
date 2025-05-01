using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
/// <summary>
/// Base class for clickable UI elements
/// </summary>
public class Clickable : MonoBehaviour, IPointerClickHandler, IInteractiveElement
{
    [SerializeField] protected bool isActive = true;
    [SerializeField] protected float clickCooldown = 0.5f;
    [SerializeField] protected AudioSource clickSound;

    public Func<bool> CanInteractCheck { get; set; }

    
    protected float lastClickTime;
    
    public virtual bool CanInteract() {
    return (CanInteractCheck?.Invoke() ?? true) && 
           isActive && 
           Time.time - lastClickTime >= clickCooldown;
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
    
    public virtual void OnPointerClick(PointerEventData eventData)
    {
        if (!CanInteract())
            return;
            
        lastClickTime = Time.time;
        
        if (clickSound != null)
        {
            clickSound.Play();
        }
        
        OnInteractionStart();
        OnClick();
        OnInteractionEnd();
    }
    
    protected virtual void OnClick()
    {
        // Override in derived classes
    }
}