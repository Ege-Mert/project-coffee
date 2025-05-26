using UnityEngine;
using ProjectCoffee.Interaction.Helpers;

namespace ProjectCoffee.Items
{
    /// <summary>
    /// Ensures all draggable items have DraggableStateManager components
    /// </summary>
    public class DraggableItemInitializer : MonoBehaviour
    {
        void Awake()
        {
            // Check if this is a draggable item
            var draggable = GetComponent<Draggable>();
            if (draggable != null)
            {
                // Ensure it has a state manager
                var stateManager = GetComponent<DraggableStateManager>();
                if (stateManager == null)
                {
                    stateManager = gameObject.AddComponent<DraggableStateManager>();
                    Debug.Log($"[DraggableItemInitializer] Added DraggableStateManager to {gameObject.name} on awake");
                }
            }
        }
    }
}
