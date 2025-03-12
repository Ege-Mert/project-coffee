using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
/// <summary>
/// Base class for draggable UI elements
/// </summary>
public class DraggableUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IInteractiveElement
{
    [SerializeField] protected bool returnToOriginalPositionOnFail = true;
    [SerializeField] protected Canvas parentCanvas;
    [SerializeField] protected Image image;
    // [SerializeField] protected AudioSource dragSound;
    // [SerializeField] protected AudioSource dropSound;
    
    protected RectTransform rectTransform;
    protected Vector2 originalPosition;
    protected Transform originalParent;
    protected CanvasGroup canvasGroup;
    protected bool isDragging = false;
    
    protected virtual void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // FIXED: More robust Canvas finding
        if (parentCanvas == null)
        {
            // Try to find canvas in parents
            parentCanvas = GetComponentInParent<Canvas>();
            
            // If still null, try to find it in the scene
            if (parentCanvas == null)
            {
                Canvas[] canvases = FindObjectsOfType<Canvas>();
                if (canvases.Length > 0)
                {
                    // Find the main canvas (typically the one with "MainCanvas" name or lowest in sorting order)
                    foreach (Canvas canvas in canvases)
                    {
                        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                        {
                            parentCanvas = canvas;
                            Debug.Log($"Found canvas: {canvas.name} for draggable {gameObject.name}");
                            break;
                        }
                    }
                    
                    // If we still don't have a canvas, just use the first one
                    if (parentCanvas == null && canvases.Length > 0)
                    {
                        parentCanvas = canvases[0];
                        Debug.Log($"Using fallback canvas: {parentCanvas.name} for draggable {gameObject.name}");
                    }
                }
                else
                {
                    Debug.LogError($"No Canvas found in the scene! Draggable {gameObject.name} won't work properly.");
                }
            }
            else
            {
                Debug.Log($"Found parent canvas: {parentCanvas.name} for draggable {gameObject.name}");
            }
        }
        
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
    }
    
    public virtual bool CanInteract()
    {
        // Override in derived classes to add specific conditions
        return true;
    }
    
    public virtual void OnInteractionStart()
    {
        // Visual feedback when interaction starts
        transform.DOScale(1.1f, 0.2f);
    }
    
    public virtual void OnInteractionEnd()
    {
        // Visual feedback when interaction ends
        transform.DOScale(1.0f, 0.2f);
    }
    
    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanInteract())
        {
            eventData.pointerDrag = null;
            return;
        }
        
        // FIXED: Check if we have a valid canvas
        if (parentCanvas == null)
        {
            Debug.LogError($"Cannot drag {gameObject.name}: No Canvas reference found!");
            eventData.pointerDrag = null;
            return;
        }
        
        isDragging = true;
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        
        // Make it transparent and pass through raycast while dragging
        canvasGroup.alpha = 0.8f;
        canvasGroup.blocksRaycasts = false;
        
        // Move to the top of the hierarchy for proper rendering order
        transform.SetAsLastSibling();
        
        // // Play sound
        // if (dragSound != null)
        // {
        //     dragSound.Play();
        // }
        
        OnInteractionStart();
    }
    
    public virtual void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || parentCanvas == null)
            return;
        
        // Update position based on mouse/touch movement
        // Converting to anchored position for proper UI positioning
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            eventData.position,
            parentCanvas.worldCamera,
            out Vector2 localPosition);
        
        rectTransform.position = parentCanvas.transform.TransformPoint(localPosition);
    }
    
    public virtual void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;
        
        isDragging = false;
        
        // Restore visual properties
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        
        // Check if dropped on a drop zone
        if (eventData.pointerCurrentRaycast.gameObject != null)
        {
            DropZoneUI dropZone = eventData.pointerCurrentRaycast.gameObject.GetComponent<DropZoneUI>();
            
            if (dropZone != null && dropZone.CanAccept(this))
            {
                dropZone.OnItemDropped(this);
                
                // // Play sound
                // if (dropSound != null)
                // {
                //     dropSound.Play();
                // }
                
                OnInteractionEnd();
                return;
            }
        }
        
        // No valid drop zone found, return to original position
        if (returnToOriginalPositionOnFail)
        {
            ReturnToOriginalPosition();
        }
        
        OnInteractionEnd();
    }
    
    public virtual void ReturnToOriginalPosition()
    {
        transform.SetParent(originalParent);
        
        // Animate return
        rectTransform.DOAnchorPos(originalPosition, 0.3f).SetEase(Ease.OutBack);
    }
}