using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
/// <summary>
/// Base class for containers that hold ingredients
/// </summary>
public class Container : Draggable, IContainer
{
    protected Dictionary<string, float> contents = new Dictionary<string, float>();
    
    public virtual bool TryAddItem(string itemId, float amount = 1f)
    {
        if (contents.ContainsKey(itemId))
        {
            contents[itemId] += amount;
        }
        else
        {
            contents.Add(itemId, amount);
        }
        
        OnContentsChanged();
        return true;
    }
    
    public virtual bool TryRemoveItem(string itemId, float amount = 1f)
    {
        if (contents.ContainsKey(itemId) && contents[itemId] >= amount)
        {
            contents[itemId] -= amount;
            
            if (contents[itemId] <= 0)
            {
                contents.Remove(itemId);
            }
            
            OnContentsChanged();
            return true;
        }
        
        return false;
    }
    
    public bool ContainsItem(string itemId, float minAmount = 0f)
    {
        return contents.ContainsKey(itemId) && contents[itemId] >= minAmount;
    }
    
    public float GetItemAmount(string itemId)
    {
        if (contents.TryGetValue(itemId, out float amount))
        {
            return amount;
        }
        
        return 0f;
    }
    
    public Dictionary<string, float> GetContents()
    {
        return new Dictionary<string, float>(contents);
    }
    
    public virtual void Clear()
    {
        contents.Clear();
        OnContentsChanged();
    }
    
    protected virtual void OnContentsChanged()
    {
        // Override in derived classes to update visuals
    }
}