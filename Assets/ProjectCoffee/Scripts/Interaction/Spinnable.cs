using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Base class for spinnable UI elements
/// </summary>
public class Spinnable : InteractiveElementBase, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] protected float requiredSpinAngle = 360f; // One full rotation
    [SerializeField] protected RectTransform rotationCenter;
    [SerializeField] protected Image spinProgressIndicator;
    [SerializeField] protected AudioSource spinProgressSound;
    [SerializeField] protected AudioSource spinCompletedSound;
    [SerializeField] protected bool resetOnRelease = false;
    [SerializeField] protected bool allowBothDirections = true; // Setting for spin direction
    
    protected bool isSpinning = false;
    protected Vector2 lastDragPosition;
    protected float currentSpinAngle = 0f;
    protected int spinCount = 0;
    
    public int SpinCount => spinCount;
    
    public event Action<int> OnSpinCompleted;
    
    protected virtual void Awake()
    {
        // Removed the base.Awake() call since InteractiveElementBase doesn't have an Awake method
        
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
        Debug.Log($"SpinnableUI initialized: {gameObject.name}");
        Debug.Log($"Required spin angle: {requiredSpinAngle}");
        Debug.Log($"Allow both directions: {allowBothDirections}");
    }
    
    public override void OnInteractionStart()
    {
        base.OnInteractionStart();
        
        // Additional spinnable-specific interaction start logic
        if (spinProgressIndicator != null)
        {
            spinProgressIndicator.fillAmount = 0f;
            spinProgressIndicator.gameObject.SetActive(true);
        }
    }
    
    public override void OnInteractionEnd()
    {
        base.OnInteractionEnd();
        
        // Additional spinnable-specific interaction end logic
        if (spinProgressIndicator != null)
        {
            spinProgressIndicator.gameObject.SetActive(false);
        }
    }
    
    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if (!CanInteract())
            return;
            
        Debug.Log("Spinnable pointer down");
        isSpinning = true;
        currentSpinAngle = 0f;
        lastDragPosition = eventData.position;
        
        OnInteractionStart();
    }
    
    public virtual void OnPointerUp(PointerEventData eventData)
    {
        if (!isSpinning)
            return;
            
        Debug.Log("Spinnable pointer up");
        isSpinning = false;
        
        if (resetOnRelease)
        {
            // Reset rotation
            rotationCenter.rotation = Quaternion.identity;
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
        
        // Update visual rotation
        rotationCenter.Rotate(0, 0, angleDelta);
        
        // Update total angle and check for completion
        if (Mathf.Abs(angleDelta) > 1f) // Minimum threshold to count as spinning
        {
            // Check direction based on setting
            bool validDirection = allowBothDirections || angleDelta > 0;
            
            if (validDirection)
            {
                // Count the absolute value of the angle change if both directions are allowed
                float angleToAdd = allowBothDirections ? Mathf.Abs(angleDelta) : angleDelta;
                currentSpinAngle += angleToAdd;
                
                // Update progress indicator
                if (spinProgressIndicator != null)
                {
                    float progress = (currentSpinAngle % requiredSpinAngle) / requiredSpinAngle;
                    spinProgressIndicator.fillAmount = progress;
                    
                    // Play progress sound on increments
                    if (progress > 0.25f && progress < 0.35f || 
                        progress > 0.5f && progress < 0.6f || 
                        progress > 0.75f && progress < 0.85f)
                    {
                        InteractionFeedbackHelper.PlaySound(spinProgressSound);
                    }
                }
                
                // Check if completed a full rotation
                if (currentSpinAngle >= requiredSpinAngle)
                {
                    spinCount++;
                    currentSpinAngle = currentSpinAngle % requiredSpinAngle;
                    OnSpinComplete();
                }
            }
        }
        
        lastDragPosition = currentPosition;
    }
    
    protected virtual void OnSpinComplete()
    {
        Debug.Log($"Spin completed! Spin count: {spinCount}");
        
        // Play completion sound
        InteractionFeedbackHelper.PlaySound(spinCompletedSound);
        
        // Add visual feedback
        InteractionFeedbackHelper.PlayBounceAnimation(transform);
        
        // Notify listeners
        OnSpinCompleted?.Invoke(spinCount);
    }
    
    public void ResetSpinCount()
    {
        spinCount = 0;
    }
}