using UnityEngine;
using UnityEditor;

public class RagdollCleaner : EditorWindow
{
    [MenuItem("Tools/Ragdoll/Clear Ragdoll Components")]
    private static void ClearRagdoll()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Clear Ragdoll", "Please select the character root GameObject in the Hierarchy.", "OK");
            return;
        }

        bool confirm = EditorUtility.DisplayDialog(
            "Clear Ragdoll Components",
            $"Are you sure you want to remove all Rigidbody, CharacterJoint, and Collider components from the children bones of '{selected.name}'?",
            "Yes, Clear",
            "Cancel"
        );

        if (!confirm) return;

        // Gather all components on child transforms (excluding the root itself to preserve AI/Controller setups)
        CharacterJoint[] joints = selected.GetComponentsInChildren<CharacterJoint>(true);
        Rigidbody[] rigidbodies = selected.GetComponentsInChildren<Rigidbody>(true);
        Collider[] colliders = selected.GetComponentsInChildren<Collider>(true);

        int jointsRemoved = 0;
        int rigidbodiesRemoved = 0;
        int collidersRemoved = 0;

        // Group actions for a single Undo step
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Clear Ragdoll Components");

        // 1. Remove Joints first (to avoid dependency warnings)
        foreach (var joint in joints)
        {
            if (joint != null)
            {
                Undo.DestroyObjectImmediate(joint);
                jointsRemoved++;
            }
        }

        // 2. Remove Rigidbodies on child bones
        foreach (var rb in rigidbodies)
        {
            if (rb != null)
            {
                // Skip the root object so we don't destroy main Rigidbody configs
                if (rb.gameObject != selected)
                {
                    Undo.DestroyObjectImmediate(rb);
                    rigidbodiesRemoved++;
                }
            }
        }

        // 3. Remove Colliders on child bones
        foreach (var col in colliders)
        {
            if (col != null)
            {
                // Skip the root object so we don't destroy the main character Capsule/Box Collider
                if (col.gameObject != selected)
                {
                    Undo.DestroyObjectImmediate(col);
                    collidersRemoved++;
                }
            }
        }

        EditorUtility.DisplayDialog(
            "Clear Ragdoll Complete",
            $"Successfully cleared ragdoll components from child bones:\n- Joints: {jointsRemoved}\n- Rigidbodies: {rigidbodiesRemoved}\n- Colliders: {collidersRemoved}",
            "OK"
        );
        
        Debug.Log($"[Ragdoll Cleaner] Cleared {jointsRemoved} Joints, {rigidbodiesRemoved} Rigidbodies, and {collidersRemoved} Colliders from the hierarchy of '{selected.name}'.");
    }
}
