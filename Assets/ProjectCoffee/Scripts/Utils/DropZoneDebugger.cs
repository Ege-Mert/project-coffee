using UnityEngine;
using ProjectCoffee.Interaction.Helpers;
using System.Collections.Generic;

namespace ProjectCoffee.Utils
{
    /// <summary>
    /// Debug helper to visualize drop zone states and configuration
    /// </summary>
    public class DropZoneDebugger : MonoBehaviour
    {
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private float refreshInterval = 0.5f;
        
        private float lastRefreshTime;
        private List<DropZoneInfo> dropZones = new List<DropZoneInfo>();
        
        private class DropZoneInfo
        {
            public DropZone zone;
            public DropZoneItemTracker tracker;
            public string name;
            public string type;
            public bool hasItem;
            public string itemName;
        }
        
        void Start()
        {
            RefreshDropZoneList();
        }
        
        void Update()
        {
            if (Time.time - lastRefreshTime > refreshInterval)
            {
                RefreshDropZoneList();
                lastRefreshTime = Time.time;
            }
        }
        
        void RefreshDropZoneList()
        {
            dropZones.Clear();
            
            // Find all drop zones in the scene
            var allZones = FindObjectsOfType<DropZone>();
            
            foreach (var zone in allZones)
            {
                var info = new DropZoneInfo
                {
                    zone = zone,
                    tracker = zone.GetComponent<DropZoneItemTracker>(),
                    name = zone.gameObject.name,
                    type = zone.GetType().Name
                };
                
                if (info.tracker != null)
                {
                    info.hasItem = info.tracker.HasItem;
                    info.itemName = info.tracker.CurrentItem?.name ?? "None";
                }
                else
                {
                    info.hasItem = zone.transform.childCount > 0;
                    info.itemName = info.hasItem ? zone.transform.GetChild(0).name : "None";
                }
                
                dropZones.Add(info);
            }
        }
        
        void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 400, 600));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("Drop Zone Debug Info", GUI.skin.GetStyle("label"));
            GUILayout.Space(10);
            
            foreach (var info in dropZones)
            {
                GUILayout.BeginVertical("box");
                
                // Zone name and type
                GUILayout.Label($"Zone: {info.name}", GUI.skin.GetStyle("boldLabel"));
                GUILayout.Label($"Type: {info.type}");
                
                // Tracker status
                if (info.tracker != null)
                {
                    GUILayout.Label("✓ Has Item Tracker", GUI.skin.GetStyle("label"));
                }
                else
                {
                    GUI.color = Color.yellow;
                    GUILayout.Label("⚠ No Item Tracker", GUI.skin.GetStyle("label"));
                    GUI.color = Color.white;
                }
                
                // Item status
                if (info.hasItem)
                {
                    GUI.color = Color.green;
                    GUILayout.Label($"Item: {info.itemName}", GUI.skin.GetStyle("label"));
                    GUI.color = Color.white;
                }
                else
                {
                    GUILayout.Label("Empty", GUI.skin.GetStyle("label"));
                }
                
                // Special info for espresso zones
                if (info.zone is EspressoMachineDropZone espressoZone)
                {
                    var slotIndex = GetSlotIndex(espressoZone);
                    GUILayout.Label($"Slot Index: {slotIndex}");
                }
                
                GUILayout.EndVertical();
                GUILayout.Space(5);
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        // Helper to get slot index via reflection (for debug purposes)
        private int GetSlotIndex(EspressoMachineDropZone zone)
        {
            var field = typeof(EspressoDropZoneBase).GetField("slotIndex", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                return (int)field.GetValue(zone);
            }
            return -1;
        }
    }
}
