using UnityEngine;

namespace ProjectCoffee.Interaction.Helpers
{
    /// <summary>
    /// Helper component to track items in drop zones and prevent overlapping
    /// </summary>
    public class DropZoneItemTracker : MonoBehaviour
    {
        private Draggable currentItem;
        
        /// <summary>
        /// Check if the drop zone currently has an item
        /// </summary>
        public bool HasItem => currentItem != null;
        
        /// <summary>
        /// Get the current item in the drop zone
        /// </summary>
        public Draggable CurrentItem => currentItem;
        
        /// <summary>
        /// Validate if a new item can be accepted
        /// </summary>
        public bool CanAcceptItem(Draggable item)
        {
            if (item == null) return false;
            
            // Allow the same item that's already here (for validation during OnEndDrag)
            if (currentItem == item)
            {
                Debug.Log($"[DropZoneItemTracker] {gameObject.name} allowing same item {item.name}");
                return true;
            }
            
            // Reject if we already have a different item
            if (currentItem != null)
            {
                Debug.Log($"[DropZoneItemTracker] {gameObject.name} already has item {currentItem.name}, rejecting {item.name}");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Set the current item (called by drop zone when item is placed)
        /// </summary>
        public void SetItem(Draggable item)
        {
            currentItem = item;
            Debug.Log($"[DropZoneItemTracker] {gameObject.name} now has item: {item?.name}");
        }
        
        /// <summary>
        /// Clear the current item (called when item is removed)
        /// </summary>
        public void ClearItem()
        {
            if (currentItem != null)
            {
                Debug.Log($"[DropZoneItemTracker] {gameObject.name} clearing item: {currentItem.name}");
                currentItem = null;
            }
        }
        
        private void OnTransformChildrenChanged()
        {
            // Check if our tracked item is still a child
            if (currentItem != null && currentItem.transform.parent != transform)
            {
                ClearItem();
            }
        }
    }
}
