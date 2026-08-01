#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EnemySniperSpawner))]
[CanEditMultipleObjects]
public class EnemySniperSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default fields (Prefab, Spawn Point, etc.)
        DrawDefaultInspector();

        EnemySniperSpawner spawner = (EnemySniperSpawner)target;

        GUILayout.Space(15);
        
        // Show context-aware setup buttons
        if (spawner.DesignatedPoint == null)
        {
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Create Designated Point (Waypoint)", GUILayout.Height(30)))
            {
                CreateDesignatedPoint(spawner);
            }
        }
        else
        {
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Select Designated Point", GUILayout.Height(30)))
            {
                Selection.activeGameObject = spawner.DesignatedPoint.gameObject;
            }
        }
        
        GUI.backgroundColor = Color.white;
    }

    private void CreateDesignatedPoint(EnemySniperSpawner spawner)
    {
        // 1. Create designated point child object
        GameObject pointObj = new GameObject($"{spawner.gameObject.name}_DesignatedPoint");
        pointObj.transform.SetParent(spawner.transform);
        
        // 2. Default offset of 5 units forward
        pointObj.transform.localPosition = new Vector3(0, 0, 5f);
        
        // 3. Register created object for Undo compatibility
        Undo.RegisterCreatedObjectUndo(pointObj, "Create Sniper Designated Point");
        
        // 4. Save spawner configuration state for undo
        Undo.RecordObject(spawner, "Assign Designated Point");
        spawner.SetDesignatedPoint(pointObj.transform);
        
        // 5. Tell Unity changes have been made to dirty the scene
        EditorUtility.SetDirty(spawner);
        
        // 6. Select the point in Scene GUI immediately so they can drag it
        Selection.activeGameObject = pointObj;
        
        Debug.Log($"[Sniper Spawner Tool] Successfully created and assigned waypoint target '{pointObj.name}'");
    }

    private void OnSceneGUI()
    {
        EnemySniperSpawner spawner = (EnemySniperSpawner)target;

        if (spawner.SpawnPoint != null && spawner.DesignatedPoint != null)
        {
            // 1. Draw dotted indicator line connecting spawn and perch locations
            Handles.color = Color.cyan;
            Handles.DrawDottedLine(spawner.SpawnPoint.position, spawner.DesignatedPoint.position, 4.0f);
            
            // 2. Draw label boxes in 3D scene space
            Handles.Label(spawner.SpawnPoint.position + Vector3.up * 0.5f, "Spawn Point", EditorStyles.boldLabel);
            Handles.Label(spawner.DesignatedPoint.position + Vector3.up * 0.5f, "Sniper Designated Waypoint", EditorStyles.boldLabel);
            
            // 3. Draw solid circular pads at points
            Handles.DrawSolidDisc(spawner.SpawnPoint.position, Vector3.up, 0.3f);
            Handles.DrawSolidDisc(spawner.DesignatedPoint.position, Vector3.up, 0.3f);
        }
    }
}
#endif
