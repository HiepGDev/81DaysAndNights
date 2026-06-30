using UnityEngine;
using UnityEditor;

public class CopyComponentsRecursively : EditorWindow
{
    private GameObject sourceRoot;
    private GameObject targetRoot;

    [MenuItem("Tools/Copy Components Recursively")]
    public static void ShowWindow()
    {
        GetWindow<CopyComponentsRecursively>("Copy Components");
    }

    private void OnGUI()
    {
        GUILayout.Label("Copy Colliders, Rigidbodies, & Scripts by Bone Name", EditorStyles.boldLabel);
        sourceRoot = (GameObject)EditorGUILayout.ObjectField("Old Soldier Root", sourceRoot, typeof(GameObject), true);
        targetRoot = (GameObject)EditorGUILayout.ObjectField("New Soldier Root", targetRoot, typeof(GameObject), true);

        if (GUILayout.Button("Copy Components"))
        {
            if (sourceRoot == null || targetRoot == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign both roots!", "OK");
                return;
            }

            Copy(sourceRoot.transform, targetRoot.transform);
            EditorUtility.DisplayDialog("Success", "Components copied successfully!", "OK");
        }
    }

    private void Copy(Transform src, Transform dst)
    {
        // Copy components from source bone to destination bone
        foreach (var comp in src.GetComponents<Component>())
        {
            // Skip transform and other fundamental components
            if (comp is Transform || comp is Animator || comp is SkinnedMeshRenderer) continue;

            UnityEditorInternal.ComponentUtility.CopyComponent(comp);
            var existing = dst.GetComponent(comp.GetType());

            if (existing != null)
            {
                UnityEditorInternal.ComponentUtility.PasteComponentValues(existing);
            }
            else
            {
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(dst.gameObject);
            }
        }

        // Recurse into children matching by bone name
        foreach (Transform srcChild in src)
        {
            Transform dstChild = dst.Find(srcChild.name);
            if (dstChild != null)
            {
                Copy(srcChild, dstChild);
            }
        }
    }
}
