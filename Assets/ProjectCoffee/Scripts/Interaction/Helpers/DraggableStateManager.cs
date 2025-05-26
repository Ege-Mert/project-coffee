using UnityEngine;
using UnityEngine.UI;

namespace ProjectCoffee.Interaction.Helpers
{
    /// <summary>
    /// Manages the visual and interaction state of draggable items during processing
    /// </summary>
    public class DraggableStateManager : MonoBehaviour
    {
        private Draggable draggable;
        private CanvasGroup canvasGroup;
        private Image image;
        
        // Original state storage
        private bool originalDraggableEnabled;
        private float originalAlpha;
        private Color originalColor;
        
        // Current state tracking
        private bool isCurrentlyProcessing = false;
        private bool hasStoredOriginalState = false;
        
        void Awake()
        {
            draggable = GetComponent<Draggable>();
            canvasGroup = GetComponent<CanvasGroup>();
            image = GetComponent<Image>();
            
            // Store original state immediately
            StoreOriginalState();
        }
        
        private void StoreOriginalState()
        {
            if (hasStoredOriginalState) return;
            
            if (draggable != null)
            {
                originalDraggableEnabled = draggable.enabled;
            }
            
            if (canvasGroup != null)
            {
                originalAlpha = canvasGroup.alpha;
            }
            else if (image != null)
            {
                originalColor = image.color;
                originalAlpha = originalColor.a;
            }
            
            hasStoredOriginalState = true;
            Debug.Log($"[DraggableStateManager] Stored original state for {gameObject.name}: draggable={originalDraggableEnabled}, alpha={originalAlpha}");
        }
        
        /// <summary>
        /// Force store original state with specific values - used when adding state manager during processing
        /// </summary>
        public void ForceStoreOriginalState(bool forceDraggableEnabled)
        {
            // Reset the flag to allow storing again
            hasStoredOriginalState = false;
            
            // Temporarily set draggable to the forced state
            bool currentDraggableState = draggable?.enabled ?? true;
            if (draggable != null)
            {
                draggable.enabled = forceDraggableEnabled;
            }
            
            // Store the state
            StoreOriginalState();
            
            // Restore the actual current state
            if (draggable != null)
            {
                draggable.enabled = currentDraggableState;
            }
            
            Debug.Log($"[DraggableStateManager] Force stored original state for {gameObject.name} with draggable={forceDraggableEnabled}");
        }
        
        /// <summary>
        /// Set the processing state of this item
        /// </summary>
        public void SetProcessingState(bool isProcessing)
        {
            // Prevent redundant state changes
            if (isCurrentlyProcessing == isProcessing)
            {
                return;
            }
            
            isCurrentlyProcessing = isProcessing;
            
            if (isProcessing)
            {
                ApplyProcessingState();
            }
            else
            {
                RestoreOriginalState();
            }
        }
        
        private void ApplyProcessingState()
        {
            // Store current state if not already done
            if (!hasStoredOriginalState)
            {
                StoreOriginalState();
            }
            
            // Disable dragging
            if (draggable != null)
            {
                draggable.enabled = false;
            }
            
            // Make semi-transparent
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.5f;
            }
            else if (image != null)
            {
                Color processColor = originalColor;
                processColor.a = 0.5f;
                image.color = processColor;
            }
            
            Debug.Log($"[DraggableStateManager] {gameObject.name} set to processing state");
        }
        
        public void RestoreOriginalState()
        {
            if (!hasStoredOriginalState)
            {
                Debug.LogWarning($"[DraggableStateManager] No original state stored for {gameObject.name}");
                return;
            }
            
            // Restore dragging
            if (draggable != null)
            {
                draggable.enabled = originalDraggableEnabled;
            }
            
            // Restore transparency
            if (canvasGroup != null)
            {
                canvasGroup.alpha = originalAlpha;
            }
            else if (image != null)
            {
                image.color = originalColor;
            }
            
            isCurrentlyProcessing = false;
            Debug.Log($"[DraggableStateManager] {gameObject.name} restored to original state");
        }
        
        /// <summary>
        /// Force reset to ensure item is in normal state - more aggressive than normal restore
        /// </summary>
        public void ForceReset()
        {
            Debug.Log($"[DraggableStateManager] Force reset for {gameObject.name}");
            
            // Force dragging enabled
            if (draggable != null)
            {
                draggable.enabled = true;
            }
            
            // Force full opacity
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
            else if (image != null)
            {
                Color resetColor = image.color;
                resetColor.a = 1f;
                image.color = resetColor;
            }
            
            // Reset state tracking
            isCurrentlyProcessing = false;
            
            Debug.Log($"[DraggableStateManager] {gameObject.name} force reset completed");
        }
        
        /// <summary>
        /// Check if currently in processing state
        /// </summary>
        public bool IsProcessing => isCurrentlyProcessing;
    }
}
