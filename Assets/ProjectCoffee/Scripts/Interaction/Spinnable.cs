using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Spinnable : InteractiveElementBase, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] protected float requiredSpinAngle = 360f;
    [SerializeField] protected RectTransform rotationCenter;
    [SerializeField] protected Image spinProgressIndicator;
    [SerializeField] protected AudioSource spinProgressSound;
    [SerializeField] protected AudioSource spinCompletedSound;
    [SerializeField] protected bool resetOnSpinComplete = false; // Changed default to false
    [SerializeField] protected bool allowBothDirections = true;
    
    protected bool isSpinning = false;
    protected Vector2 lastDragPosition;
    protected float currentSpinAngle = 0f;
    protected int spinCount = 0;
    protected Quaternion originalRotation;
    
    public int SpinCount => spinCount;
    public event Action<int> OnSpinCompleted;
    
    protected virtual void Awake()
    {
        if (rotationCenter == null)
            rotationCenter = GetComponent<RectTransform>();
        
        originalRotation = rotationCenter.rotation;
        
        if (spinProgressIndicator != null)
        {
            spinProgressIndicator.fillAmount = 0f;
            spinProgressIndicator.gameObject.SetActive(false);
        }
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
        if (!CanInteract()) return;
            
        isSpinning = true;
        // Don't reset currentSpinAngle - let user continue from where they left off
        lastDragPosition = eventData.position;
        
        OnInteractionStart();
    }
    
    public virtual void OnPointerUp(PointerEventData eventData)
    {
        if (!isSpinning) return;
            
        isSpinning = false;
        
        // Don't reset rotation here - let the user continue from where they stopped
        OnInteractionEnd();
    }
    
    public virtual void OnDrag(PointerEventData eventData)
    {
        if (!isSpinning) return;
            
        Vector2 currentPosition = eventData.position;
        Vector2 centerPosition = RectTransformUtility.WorldToScreenPoint(null, rotationCenter.position);
        
        Vector2 previousVector = lastDragPosition - centerPosition;
        Vector2 currentVector = currentPosition - centerPosition;
        
        float previousAngle = Mathf.Atan2(previousVector.y, previousVector.x) * Mathf.Rad2Deg;
        float currentAngle = Mathf.Atan2(currentVector.y, currentVector.x) * Mathf.Rad2Deg;
        
        float angleDelta = Mathf.DeltaAngle(previousAngle, currentAngle);
        
        rotationCenter.Rotate(0, 0, angleDelta);
        
        if (Mathf.Abs(angleDelta) > 1f)
        {
            bool validDirection = allowBothDirections || angleDelta > 0;
            
            if (validDirection)
            {
                float angleToAdd = allowBothDirections ? Mathf.Abs(angleDelta) : angleDelta;
                currentSpinAngle += angleToAdd;
                
                if (spinProgressIndicator != null)
                {
                    float progress = (currentSpinAngle % requiredSpinAngle) / requiredSpinAngle;
                    spinProgressIndicator.fillAmount = progress;
                }
                
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
        InteractionFeedbackHelper.PlaySound(spinCompletedSound);
        InteractionFeedbackHelper.PlayBounceAnimation(transform);
        OnSpinCompleted?.Invoke(spinCount);
        
        // Fixed: Only reset rotation if explicitly requested (default is now false)
        if (resetOnSpinComplete)
        {
            ResetRotation();
        }
    }
    
    public void ResetSpinCount()
    {
        spinCount = 0;
        currentSpinAngle = 0f;
        // Don't automatically reset rotation when resetting spin count
    }
    
    public void ResetRotation()
    {
        if (rotationCenter != null)
            rotationCenter.rotation = originalRotation;
        currentSpinAngle = 0f; // Also reset progress when resetting rotation
    }
    
    // Added: Method to reset both spin count and rotation if needed
    public void ResetCompletely()
    {
        spinCount = 0;
        currentSpinAngle = 0f;
        if (rotationCenter != null)
            rotationCenter.rotation = originalRotation;
    }
}