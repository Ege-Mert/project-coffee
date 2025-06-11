// using UnityEngine;
// using UnityEditor;
// using System.Collections.Generic;
// using ProjectCoffee.Machines;
// using ProjectCoffee.Machines.Dosing;

// /// <summary>
// /// Editor script to validate that the Gramming Machine has all required components for upgrades
// /// </summary>
// [CustomEditor(typeof(DosingMachine))]
// public class GrammingMachineUpgradeValidator : Editor
// {
//     public override void OnInspectorGUI()
//     {
//         DrawDefaultInspector();
        
//         DosingMachine grammingMachine = (DosingMachine)target;
        
//         // Check for required components
//         EditorGUILayout.Space();
//         EditorGUILayout.LabelField("Upgrade System Validation", EditorStyles.boldLabel);
        
//         // Get all serialized properties we want to check
//         SerializedProperty holdableButton = serializedObject.FindProperty("grammingButton");
//         SerializedProperty autoButton = serializedObject.FindProperty("autoDoseButton");
//         SerializedProperty autoIndicator = serializedObject.FindProperty("autoDosingIndicator");
        
//         List<string> missingItems = new List<string>();
        
//         if (holdableButton.objectReferenceValue == null)
//         {
//             missingItems.Add("Gramming Button (Holdable) is missing. Required for Level 0 interactions.");
//         }
        
//         if (autoButton.objectReferenceValue == null)
//         {
//             missingItems.Add("Auto Dose Button is missing. Required for Level 1 interactions.");
//         }
        
//         if (autoIndicator.objectReferenceValue == null)
//         {
//             missingItems.Add("Auto Dosing Indicator is missing. Required for Level 2 visual feedback.");
//         }
        
//         if (missingItems.Count > 0)
//         {
//             EditorGUILayout.HelpBox("Upgrade system validation detected missing elements:", MessageType.Warning);
//             foreach (var item in missingItems)
//             {
//                 EditorGUILayout.LabelField("• " + item, EditorStyles.wordWrappedLabel);
//             }
//         }
//         else
//         {
//             EditorGUILayout.HelpBox("All required upgrade components are present.", MessageType.Info);
//         }
        
//         // Add button to help configure the upgrade-specific objects
//         if (GUILayout.Button("Setup Upgrade UI Elements"))
//         {
//             // This could auto-create the missing elements, or position them correctly
//             // For now, just focus the hierarchy view on the GameObject
//             EditorGUIUtility.PingObject(grammingMachine.gameObject);
//             EditorUtility.DisplayDialog("Setup Instructions", 
//                 "To complete the Gramming Machine upgrade setup:\n\n" +
//                 "1. Ensure the holdable Gramming Button is assigned (Level 0)\n" +
//                 "2. Create and assign the Auto Dose Button (Level 1)\n" +
//                 "3. Create and assign the Auto Dosing Indicator (Level 2)\n\n" +
//                 "Check the documentation for more detailed setup instructions.",
//                 "OK");
//         }
//     }
// }
