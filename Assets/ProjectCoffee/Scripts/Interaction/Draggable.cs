using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Base class for draggable UI elements
/// </summary>
public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] public bool returnToOriginalPositionOnFail = true;
    [SerializeField] protected Canvas parentCanvas;
    [SerializeField] protected Image image;
    [SerializeField] protected AudioSource dragSound;
    [SerializeField] protected AudioSource dropSound;
    [SerializeField] protected bool debugMode = true;
    
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
        
        // More robust Canvas finding
        if (parentCanvas == null)
        {
            FindParentCanvas();
        }
        
        SaveOriginalState();
    }
    
    private void FindParentCanvas()
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
                        DebugLog($"Found canvas: {canvas.name} for draggable {gameObject.name}");
                        break;
                    }
                }
                
                // If we still don't have a canvas, just use the first one
                if (parentCanvas == null && canvases.Length > 0)
                {
                    parentCanvas = canvases[0];
                    DebugLog($"Using fallback canvas: {parentCanvas.name} for draggable {gameObject.name}");
                }
            }
            else
            {
                Debug.LogError($"No Canvas found in the scene! Draggable {gameObject.name} won't work properly.");
            }
        }
        else
        {
            DebugLog($"Found parent canvas: {parentCanvas.name} for draggable {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Save the current state (position and parent) for returning later if needed
    /// </summary>
    public void SaveOriginalState()
    {
        if (rectTransform == null) return;
        
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        
        DebugLog($"Saved original state for {gameObject.name}: Position={originalPosition}, Parent={originalParent?.name ?? "null"}");
    }
    
    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        DebugLog($"Begin Drag - {gameObject.name}");
        
        if (!CanInteract())
        {
            eventData.pointerDrag = null;
            return;
        }
        
        // Check if we have a valid canvas
        if (parentCanvas == null)
        {
            Debug.LogError($"Cannot drag {gameObject.name}: No Canvas reference found!");
            eventData.pointerDrag = null;
            return;
        }
        
        isDragging = true;
        
        // Save current state as original for return if needed
        SaveOriginalState();
        
        // Make it transparent and pass through raycast while dragging
        canvasGroup.alpha = 0.8f;
        canvasGroup.blocksRaycasts = false;
        
        // Move to the top of the hierarchy for proper rendering order
        transform.SetAsLastSibling();
        
        // Play sound
        if (dragSound != null && dragSound.isActiveAndEnabled)
        {
            dragSound.Play();
        }
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
        
        DebugLog($"End Drag - {gameObject.name}");
        
        isDragging = false;
        
        // Restore visual properties
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        
        bool validDropPerformed = false;
        DropZone dropZone = null;
        
        // Check for drop zone under the pointer
        if (eventData.pointerCurrentRaycast.gameObject != null)
        {
            // Try to get the DropZone directly from the hit object
            dropZone = eventData.pointerCurrentRaycast.gameObject.GetComponent<DropZone>();
            
            // If no DropZone found on the direct hit, try to find it in the parents
            if (dropZone == null)
            {
                dropZone = eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<DropZone>();
                if (dropZone != null)
                {
                    DebugLog($"Found drop zone {dropZone.name} in parent hierarchy");
                }
            }
            
            if (dropZone != null)
            {
                DebugLog($"Drop zone found: {dropZone.name}, Checking if can accept");
                
                bool canAccept = dropZone.CanAccept(this);
                DebugLog($"DropZone.CanAccept result: {canAccept}");
                
                if (canAccept)
                {
                    DebugLog($"Calling OnItemDropped on {dropZone.name}");
                    dropZone.OnItemDropped(this);
                    validDropPerformed = true;
                    
                    // Play sound
                    if (dropSound != null && dropSound.isActiveAndEnabled)
                    {
                        dropSound.Play();
                    }
                }
                else
                {
                    DebugLog($"Drop zone {dropZone.name} rejected the drop");
                }
            }
            else
            {
                DebugLog($"No drop zone found at drop position");
            }
        }
        
        // If dropped on an invalid zone or no zone at all, return to original position
        if (!validDropPerformed && returnToOriginalPositionOnFail)
        {
            DebugLog($"No valid drop. Returning to original position: {originalPosition}");
            StartCoroutine(ReturnToOriginalPositionDelayed());
        }
    }
    
    protected IEnumerator ReturnToOriginalPositionDelayed()
    {
        // Small delay to make sure any OnTransformChildrenChanged events have finished
        yield return new WaitForEndOfFrame();
        
        ReturnToOriginalPosition();
    }
    
    public virtual void ReturnToOriginalPosition()
    {
        if (originalParent == null)
        {
            Debug.LogError($"Cannot return {gameObject.name} to original position: originalParent is null!");
            return;
        }
        
        DebugLog($"Returning {gameObject.name} to original position: {originalPosition} under parent {originalParent.name}");
        
        // Return to original parent if not already there
        transform.SetParent(originalParent);
        
        // Immediately set the position to make sure it takes effect
        rectTransform.anchoredPosition = originalPosition;
        
        // Then animate it slightly to give feedback
        rectTransform.DOPunchPosition(Vector3.one * 5f, 0.3f, 10, 1f)
            .OnComplete(() => {
                DebugLog($"{gameObject.name} returned to original position");
                // Make sure the position is correct after animation
                rectTransform.anchoredPosition = originalPosition;
            });
    }
    
    protected virtual bool CanInteract()
    {
        // Check if the component is active and enabled
        return isActiveAndEnabled;
    }
    
    protected void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[Draggable] {message}");
        }
    }
}