using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public class DropZoneUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] protected bool isActive = true;
    [SerializeField] protected List<string> acceptedTypes = new List<string>();
    [SerializeField] protected Image highlightImage;
    [SerializeField] protected Color validHighlightColor = new Color(0.5f, 1f, 0.5f, 0.5f);
    [SerializeField] protected Color invalidHighlightColor = new Color(1f, 0.5f, 0.5f, 0.5f);

    public Func<object, bool> AcceptPredicate { get; set; }

    public virtual bool CanAccept(DraggableUI item)
    {
        if (!isActive)
            return false;

        return AcceptPredicate?.Invoke(item) ?? false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isActive)
            return;

        if (eventData.pointerDrag != null)
        {
            DraggableUI item = eventData.pointerDrag.GetComponent<DraggableUI>();
            if (item != null)
            {
                bool canAccept = CanAccept(item);
                ShowHighlight(canAccept);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideHighlight();
    }

    public virtual void OnDrop(PointerEventData eventData)
    {
        if (!isActive)
            return;

        HideHighlight();

        if (eventData.pointerDrag != null)
        {
            DraggableUI item = eventData.pointerDrag.GetComponent<DraggableUI>();
        }
    }

    public virtual void OnItemDropped(DraggableUI item)
    {
        RectTransform itemRect = item.GetComponent<RectTransform>();
        itemRect.SetParent(transform);
        itemRect.anchoredPosition = Vector2.zero;
    }

    public virtual void OnItemRemoved(DraggableUI item)
    {
    }

    protected void ShowHighlight(bool isValid)
    {
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(true);
            highlightImage.color = isValid ? validHighlightColor : invalidHighlightColor;
        }
    }

    protected void HideHighlight()
    {
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(false);
        }
    }
}