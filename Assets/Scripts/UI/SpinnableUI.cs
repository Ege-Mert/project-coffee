using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
/// <summary>
/// Base class for spinnable UI elements
/// </summary>
public class SpinnableUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IInteractiveElement
{
    [SerializeField] protected bool isActive = true;
    [SerializeField] protected float requiredSpinAngle = 360f; // One full rotation
    [SerializeField] protected RectTransform rotationCenter;
    [SerializeField] protected Image spinProgressIndicator;
    [SerializeField] protected AudioSource spinSound;
    [SerializeField] protected bool resetOnRelease = false;
    [SerializeField] protected bool allowBothDirections = true; // Added setting for spin direction
    
    protected bool isSpinning = false;
    protected Vector2 lastDragPosition;
    protected float currentSpinAngle = 0f;
    protected int spinCount = 0;
    
    public int SpinCount => spinCount;
    
    public event Action<int> OnSpinCompleted;
    
    protected virtual void Awake()
    {
        if (rotationCenter == null)
        {
            rotationCenter = GetComponent<RectTransform>();
        }
        
        if (spinProgressIndicator != null)
        {
            spinProgressIndicator.fillAmount = 0f;
            spinProgressIndicator.gameObject.SetActive(false);
        }
        
        // Print debug info
        print($"SpinnableUI initialized: {gameObject.name}");
        print($"Required spin angle: {requiredSpinAngle}");
        print($"Allow both directions: {allowBothDirections}");
    }
    
    public virtual bool CanInteract()
    {
        return isActive;
    }
    
    public virtual void OnInteractionStart()
    {
        // Visual feedback when interaction starts
    }
    
    public virtual void OnInteractionEnd()
    {
        // Visual feedback when interaction ends
    }
    
    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if (!CanInteract())
            return;
            
        print("Spinnable pointer down");
        isSpinning = true;
        currentSpinAngle = 0f;
        lastDragPosition = eventData.position;
        
        if (spinProgressIndicator != null)
        {
            spinProgressIndicator.fillAmount = 0f;
            spinProgressIndicator.gameObject.SetActive(true);
        }
        
        OnInteractionStart();
    }
    
    public virtual void OnPointerUp(PointerEventData eventData)
    {
        if (!isSpinning)
            return;
            
        print("Spinnable pointer up");
        isSpinning = false;
        
        if (resetOnRelease)
        {
            // Reset rotation
            rotationCenter.rotation = Quaternion.identity;
        }
        
        if (spinProgressIndicator != null)
        {
            spinProgressIndicator.gameObject.SetActive(false);
        }
        
        OnInteractionEnd();
    }
    
    public virtual void OnDrag(PointerEventData eventData)
    {
        if (!isSpinning)
            return;
            
        Vector2 currentPosition = eventData.position;
        Vector2 centerPosition = RectTransformUtility.WorldToScreenPoint(null, rotationCenter.position);
        
        // Calculate angles
        Vector2 previousVector = lastDragPosition - centerPosition;
        Vector2 currentVector = currentPosition - centerPosition;
        
        float previousAngle = Mathf.Atan2(previousVector.y, previousVector.x) * Mathf.Rad2Deg;
        float currentAngle = Mathf.Atan2(currentVector.y, currentVector.x) * Mathf.Rad2Deg;
        
        // Calculate angle change (delta)
        float angleDelta = Mathf.DeltaAngle(previousAngle, currentAngle);
        
        // Debug log the angle delta
        //print($"Angle delta: {angleDelta}");
        
        // Update visual rotation
        rotationCenter.Rotate(0, 0, angleDelta);
        
        // Update total angle and check for completion
        if (Mathf.Abs(angleDelta) > 1f) // Minimum threshold to count as spinning
        {
            // Modified: Check direction based on setting
            bool validDirection = allowBothDirections || angleDelta > 0;
            
            if (validDirection)
            {
                // Count the absolute value of the angle change if both directions are allowed
                float angleToAdd = allowBothDirections ? Mathf.Abs(angleDelta) : angleDelta;
                currentSpinAngle += angleToAdd;
                
                // Update progress indicator
                if (spinProgressIndicator != null)
                {
                    spinProgressIndicator.fillAmount = (currentSpinAngle % requiredSpinAngle) / requiredSpinAngle;
                }
                
                // Check if completed a full rotation
                if (currentSpinAngle >= requiredSpinAngle)
                {
                    spinCount++;
                    currentSpinAngle = currentSpinAngle % requiredSpinAngle;
                    OnSpinComplete();
                    
                    // Play sound
                    if (spinSound != null)
                    {
                        spinSound.Play();
                    }
                }
            }
        }
        
        lastDragPosition = currentPosition;
    }
    
    protected virtual void OnSpinComplete()
    {
        print($"Spin completed! Spin count: {spinCount}");
        // Notify listeners
        OnSpinCompleted?.Invoke(spinCount);
    }
    
    public void ResetSpinCount()
    {
        spinCount = 0;
    }
}