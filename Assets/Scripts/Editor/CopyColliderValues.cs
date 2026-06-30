using UnityEngine;
using UnityEditor;

public class CopyColliderValues : EditorWindow
{
    private GameObject sourceRoot;
    private GameObject targetRoot;

    [MenuItem("Tools/Copy Collider Values")]
    public static void ShowWindow()
    {
        GetWindow<CopyColliderValues>("Copy Colliders");
    }

    private void OnGUI()
    {
        GUILayout.Label("Copy Collider Dimensions (Size/Center/Radius) by Bone Name", EditorStyles.boldLabel);
        sourceRoot = (GameObject)EditorGUILayout.ObjectField("Source (Old) Soldier", sourceRoot, typeof(GameObject), true);
        targetRoot = (GameObject)EditorGUILayout.ObjectField("Target (New) Soldier", targetRoot, typeof(GameObject), true);

        if (GUILayout.Button("Sync Collider Values"))
        {
            if (sourceRoot == null || targetRoot == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign both roots!", "OK");
                return;
            }

            int count = SyncColliders(sourceRoot.transform, targetRoot.transform);
            EditorUtility.DisplayDialog("Success", $"Synced collider values on {count} bones!", "OK");
        }
    }

    private int SyncColliders(Transform src, Transform dst)
    {
        int syncedBonesCount = 0;
        bool boneSynced = false;

        // Copy the bone GameObject's tag
        if (src.gameObject.tag != dst.gameObject.tag)
        {
            dst.gameObject.tag = src.gameObject.tag;
            boneSynced = true;
        }

        // Sync BoxColliders
        var srcBoxes = src.GetComponents<BoxCollider>();
        var dstBoxes = dst.GetComponents<BoxCollider>();
        for (int i = 0; i < Mathf.Min(srcBoxes.Length, dstBoxes.Length); i++)
        {
            dstBoxes[i].center = srcBoxes[i].center;
            dstBoxes[i].size = srcBoxes[i].size;
            dstBoxes[i].isTrigger = srcBoxes[i].isTrigger;
            dstBoxes[i].sharedMaterial = srcBoxes[i].sharedMaterial;
            boneSynced = true;
        }

        // Sync SphereColliders
        var srcSpheres = src.GetComponents<SphereCollider>();
        var dstSpheres = dst.GetComponents<SphereCollider>();
        for (int i = 0; i < Mathf.Min(srcSpheres.Length, dstSpheres.Length); i++)
        {
            dstSpheres[i].center = srcSpheres[i].center;
            dstSpheres[i].radius = srcSpheres[i].radius;
            dstSpheres[i].isTrigger = srcSpheres[i].isTrigger;
            dstSpheres[i].sharedMaterial = srcSpheres[i].sharedMaterial;
            boneSynced = true;
        }

        // Sync CapsuleColliders
        var srcCapsules = src.GetComponents<CapsuleCollider>();
        var dstCapsules = dst.GetComponents<CapsuleCollider>();
        for (int i = 0; i < Mathf.Min(srcCapsules.Length, dstCapsules.Length); i++)
        {
            dstCapsules[i].center = srcCapsules[i].center;
            dstCapsules[i].radius = srcCapsules[i].radius;
            dstCapsules[i].height = srcCapsules[i].height;
            dstCapsules[i].direction = srcCapsules[i].direction;
            dstCapsules[i].isTrigger = srcCapsules[i].isTrigger;
            dstCapsules[i].sharedMaterial = srcCapsules[i].sharedMaterial;
            boneSynced = true;
        }

        if (boneSynced) syncedBonesCount++;

        // Recurse into child bones matching by name
        foreach (Transform srcChild in src)
        {
            Transform dstChild = dst.Find(srcChild.name);
            if (dstChild != null)
            {
                syncedBonesCount += SyncColliders(srcChild, dstChild);
            }
        }

        return syncedBonesCount;
    }
}
