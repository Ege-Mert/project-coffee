using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Class for areas that can accept draggable items
/// </summary>
public class DropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] protected bool isActive = true;
    [SerializeField] protected Image highlightImage;
    [SerializeField] protected Color validHighlightColor = new Color(0.5f, 1f, 0.5f, 0.5f);
    [SerializeField] protected Color invalidHighlightColor = new Color(1f, 0.5f, 0.5f, 0.5f);
    [SerializeField] protected AudioSource validDropSound;
    [SerializeField] protected AudioSource invalidDropSound;
    [SerializeField] protected bool centerItemInZone = true;
    [SerializeField] protected bool preserveItemSize = true;
    [SerializeField] protected bool debugLogs = true;

    /// <summary>
    /// Predicate that determines if a draggable item can be accepted
    /// </summary>
    public Func<Draggable, bool> AcceptPredicate { get; set; }

    /// <summary>
    /// Check if this drop zone can accept a specific draggable item
    /// </summary>
    public virtual bool CanAccept(Draggable item)
    {
        if (!isActive || item == null)
            return false;

        return AcceptPredicate?.Invoke(item) ?? false;
    }

    /// <summary>
    /// Called when a pointer enters the drop zone
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isActive)
            return;

        if (eventData.pointerDrag != null)
        {
            Draggable item = eventData.pointerDrag.GetComponent<Draggable>();
            if (item != null)
            {
                bool canAccept = CanAccept(item);
                LogDebug($"Pointer entered with {item.name}, can accept: {canAccept}");
                ShowHighlight(canAccept);
            }
        }
    }

    /// <summary>
    /// Called when a pointer exits the drop zone
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        HideHighlight();
    }

    /// <summary>
    /// Called when an item is dropped on the drop zone
    /// </summary>
    public virtual void OnDrop(PointerEventData eventData)
    {
        if (!isActive)
            return;

        HideHighlight();

        if (eventData.pointerDrag != null)
        {
            Draggable item = eventData.pointerDrag.GetComponent<Draggable>();
            if (item != null)
            {
                bool canAccept = CanAccept(item);
                LogDebug($"Item {item.name} dropped, can accept: {canAccept}");
                
                if (canAccept)
                {
                    // Valid drop - handle it
                    OnItemDropped(item);
                    
                    // Play valid drop sound
                    if (validDropSound != null && validDropSound.isActiveAndEnabled)
                    {
                        validDropSound.Play();
                    }
                }
                else
                {
                    // Invalid drop - play sound 
                    if (invalidDropSound != null && invalidDropSound.isActiveAndEnabled)
                    {
                        invalidDropSound.Play();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Called when a valid item is dropped on this zone
    /// </summary>
    public virtual void OnItemDropped(Draggable item)
    {
        if (item == null) return;
        
        LogDebug($"Processing drop of {item.name}");
        
        // Get reference to current child, if any
        if (transform.childCount > 0)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Draggable existingItem = transform.GetChild(i).GetComponent<Draggable>();
                if (existingItem != null && existingItem != item)
                {
                    // Handle removal of existing item
                    LogDebug($"Removing existing item {existingItem.name}");
                    OnItemRemoved(existingItem);
                    existingItem.ReturnToOriginalPosition();
                }
            }
        }
        
        // Store original scale before parenting
        Vector3 originalScale = item.transform.localScale;
        
        // Set the draggable as a child of this drop zone
        LogDebug($"Setting {item.name} as child of {gameObject.name}");
        RectTransform itemRect = item.GetComponent<RectTransform>();
        itemRect.SetParent(transform);
        
        // Center the item if specified
        if (centerItemInZone)
        {
            LogDebug($"Centering {item.name} in zone");
            itemRect.anchoredPosition = Vector2.zero;
        }
        
        // Preserve the item's original size
        if (preserveItemSize)
        {
            // Restore original scale after parenting
            LogDebug($"Preserving original scale {originalScale}");
            itemRect.localScale = originalScale;
        }
        
        // Tell custom handlers about the new item
        RaiseItemDropped(item);
    }

    /// <summary>
    /// Called when an item is removed from this zone
    /// </summary>
    public virtual void OnItemRemoved(Draggable item)
    {
        if (item == null) return;
        
        LogDebug($"Item {item.name} removed from zone");
        
        // Tell custom handlers about the removal
        RaiseItemRemoved(item);
    }
    
    /// <summary>
    /// Raise custom events for item dropped
    /// </summary>
    protected virtual void RaiseItemDropped(Draggable item)
    {
        // Override in derived classes to add custom events
    }
    
    /// <summary>
    /// Raise custom events for item removed
    /// </summary>
    protected virtual void RaiseItemRemoved(Draggable item)
    {
        // Override in derived classes to add custom events
    }

    /// <summary>
    /// Show the highlight with appropriate color
    /// </summary>
    protected void ShowHighlight(bool isValid)
    {
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(true);
            highlightImage.color = isValid ? validHighlightColor : invalidHighlightColor;
        }
    }

    /// <summary>
    /// Hide the highlight
    /// </summary>
    protected void HideHighlight()
    {
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Set whether this drop zone is active
    /// </summary>
    public void SetActive(bool active)
    {
        isActive = active;
    }
    
    /// <summary>
    /// Log a debug message
    /// </summary>
    protected void LogDebug(string message)
    {
        if (debugLogs)
        {
            Debug.Log($"[DropZone:{gameObject.name}] {message}");
        }
    }
}