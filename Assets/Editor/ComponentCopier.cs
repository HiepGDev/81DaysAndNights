using UnityEngine;
using UnityEditor;

public static class ComponentCopier
{
    private static Component sourceComponent;

    [MenuItem("CONTEXT/Component/Copy Different Component Fields")]
    private static void CopyFields(MenuCommand command)
    {
        sourceComponent = command.context as Component;
        Debug.Log($"[Component Copier] Copied fields from: {sourceComponent.GetType().Name}");
    }

    [MenuItem("CONTEXT/Component/Paste Matching Fields")]
    private static void PasteFields(MenuCommand command)
    {
        Component targetComponent = command.context as Component;

        if (sourceComponent == null)
        {
            Debug.LogError("[Component Copier] No source component copied yet!");
            return;
        }

        if (targetComponent == null) return;

        // Record Undo so you can Ctrl+Z if you make a mistake
        Undo.RecordObject(targetComponent, "Paste Matching Fields");

        // Use SerializedObjects to read/write properties safely
        SerializedObject sourceSO = new SerializedObject(sourceComponent);
        SerializedObject targetSO = new SerializedObject(targetComponent);

        SerializedProperty sourceProp = sourceSO.GetIterator();
        
        // Loop through all fields of the source component
        while (sourceProp.NextVisible(true))
        {
            // Skip the internal script reference field
            if (sourceProp.name == "m_Script") continue;

            // Check if the target component has a property with the exact same name
            SerializedProperty targetProp = targetSO.FindProperty(sourceProp.name);

            // If found and the property types match, copy the value
            if (targetProp != null && targetProp.propertyType == sourceProp.propertyType)
            {
                targetSO.CopyFromSerializedProperty(sourceProp);
            }
        }

        // Apply changes to the target component
        targetSO.ApplyModifiedProperties();
        Debug.Log($"[Component Copier] Successfully transferred matching fields from {sourceComponent.GetType().Name} to {targetComponent.GetType().Name}");
    }
}
