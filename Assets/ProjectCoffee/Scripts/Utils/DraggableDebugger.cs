using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

namespace ProjectCoffee.Utils
{
    /// <summary>
    /// Debug component to diagnose dragging issues
    /// </summary>
    public class DraggableDebugger : MonoBehaviour
    {
        private Draggable draggable;
        private CanvasGroup canvasGroup;
        private bool wasReportedLastFrame = false;
        
        void Start()
        {
            draggable = GetComponent<Draggable>();
            canvasGroup = GetComponent<CanvasGroup>();
            
            if (draggable == null)
            {
                Debug.LogError($"DraggableDebugger on {gameObject.name} requires a Draggable component!");
                enabled = false;
            }
        }
        
        void Update()
        {
            // Check every second
            if (Time.frameCount % 60 == 0)
            {
                CheckDraggableState();
            }
        }
        
        void CheckDraggableState()
        {
            bool canDrag = true;
            string issues = "";
            
            // Check Draggable component
            if (draggable != null)
            {
                if (!draggable.enabled)
                {
                    canDrag = false;
                    issues += "Draggable disabled; ";
                }
                
                if (!draggable.gameObject.activeInHierarchy)
                {
                    canDrag = false;
                    issues += "GameObject not active; ";
                }
            }
            
            // Check CanvasGroup
            if (canvasGroup != null)
            {
                if (!canvasGroup.blocksRaycasts)
                {
                    canDrag = false;
                    issues += "CanvasGroup blocks raycasts OFF; ";
                }
                
                if (!canvasGroup.interactable)
                {
                    canDrag = false;
                    issues += "CanvasGroup not interactable; ";
                }
                
                if (canvasGroup.alpha < 0.1f)
                {
                    canDrag = false;
                    issues += $"CanvasGroup alpha too low ({canvasGroup.alpha}); ";
                }
            }
            
            // Check EventSystem
            if (EventSystem.current == null)
            {
                canDrag = false;
                issues += "No EventSystem found; ";
            }
            else if (!EventSystem.current.enabled)
            {
                canDrag = false;
                issues += "EventSystem disabled; ";
            }
            
            // Report issues
            if (!canDrag && !wasReportedLastFrame)
            {
                Debug.LogWarning($"[DraggableDebugger] {gameObject.name} CANNOT be dragged! Issues: {issues}");
                wasReportedLastFrame = true;
            }
            else if (canDrag && wasReportedLastFrame)
            {
                Debug.Log($"[DraggableDebugger] {gameObject.name} is now draggable again!");
                wasReportedLastFrame = false;
            }
        }
        
        // Manual check that can be called
        public void ForceCheck()
        {
            Debug.Log($"[DraggableDebugger] Force checking {gameObject.name}:");
            Debug.Log($"  - Draggable enabled: {draggable?.enabled}");
            Debug.Log($"  - GameObject active: {gameObject.activeInHierarchy}");
            Debug.Log($"  - CanvasGroup blocksRaycasts: {canvasGroup?.blocksRaycasts}");
            Debug.Log($"  - CanvasGroup interactable: {canvasGroup?.interactable}");
            Debug.Log($"  - CanvasGroup alpha: {canvasGroup?.alpha}");
            Debug.Log($"  - EventSystem present: {EventSystem.current != null}");
            Debug.Log($"  - EventSystem enabled: {EventSystem.current?.enabled}");
            
            // Check parent hierarchy
            Transform current = transform;
            while (current != null)
            {
                var parentCanvas = current.GetComponent<CanvasGroup>();
                if (parentCanvas != null)
                {
                    Debug.Log($"  - Parent CanvasGroup on {current.name}: blocksRaycasts={parentCanvas.blocksRaycasts}, interactable={parentCanvas.interactable}");
                }
                current = current.parent;
            }
        }
    }
}
