using System.Collections.Generic;

/// <summary>
/// Interface for UI elements that contain ingredients
/// </summary>
public interface IContainer
{
    bool TryAddItem(string itemId, float amount = 1f);
    bool TryRemoveItem(string itemId, float amount = 1f);
    bool ContainsItem(string itemId, float minAmount = 0f);
    float GetItemAmount(string itemId);
    void Clear();
    Dictionary<string, float> GetContents();
}