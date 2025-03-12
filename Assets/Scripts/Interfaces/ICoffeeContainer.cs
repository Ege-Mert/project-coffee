using System.Collections.Generic;

/// <summary>
/// Interface for objects that can contain coffee
/// </summary>
public interface ICoffeeContainer
{
    /// <summary>
    /// Try to add coffee to the container
    /// </summary>
    /// <param name="amount">Amount of coffee to add in grams</param>
    /// <returns>True if all coffee was added successfully</returns>
    bool TryAddCoffee(float amount);
    
    /// <summary>
    /// Try to remove coffee from the container
    /// </summary>
    /// <param name="amount">Amount of coffee to remove in grams</param>
    /// <returns>True if coffee was removed successfully</returns>
    bool TryRemoveCoffee(float amount);
    
    /// <summary>
    /// Get the current amount of coffee in the container
    /// </summary>
    /// <returns>Amount of coffee in grams</returns>
    float GetCoffeeAmount();
    
    /// <summary>
    /// Check if the container has at least the minimum amount of coffee
    /// </summary>
    /// <param name="minAmount">Minimum amount to check for</param>
    /// <returns>True if the container has at least the minimum amount</returns>
    bool HasCoffee(float minAmount = 0f);
}