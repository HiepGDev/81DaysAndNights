using UnityEngine;
using UnityEditor;

/// <summary>
/// Adds "Center Pivot On Children" to the Transform component's context menu (the ⋮
/// button in the Inspector, or right-click on the component header).
///
/// This is the closest Unity equivalent of Blender's "Set Origin > Origin to Geometry":
/// since a plain Unity GameObject doesn't have mesh data of its own, this instead
/// computes a world-space bounding box across the whole subtree of children (using
/// Renderer / Collider / Collider2D / RectTransform data where available) and moves
/// THIS object's own position to the center of that box. Every child is then repositioned
/// back to its original world position, so nothing visually moves — only the parent's
/// own pivot point relocates to a more sensible spot.
///
/// Note: this changes the Transform's *position*. It does NOT touch RectTransform.pivot
/// (the 0–1 normalized point that defines where a UI rect's local origin sits inside its
/// own rect) — that's a different, narrower operation.
///
/// Install: put this file anywhere inside a folder named "Editor" in your project
/// (e.g. Assets/Editor/CenterPivotTool.cs).
/// </summary>
public static class CenterPivotTool
{
    [MenuItem("CONTEXT/Transform/Center Pivot On Children")]
    private static void CenterPivotOnChildren(MenuCommand command)
    {
        Transform target = (Transform)command.context;

        if (target.childCount == 0)
        {
            Debug.LogWarning($"'{target.name}' has no children to center on.");
            return;
        }

        Bounds bounds = default;
        bool initialized = false;
        for (int i = 0; i < target.childCount; i++)
        {
            EncapsulateHierarchy(target.GetChild(i), ref bounds, ref initialized);
        }

        if (!initialized)
        {
            Debug.LogWarning($"Couldn't determine any bounds from the children of '{target.name}'.");
            return;
        }

        Vector3 newWorldPosition = bounds.center;

        if (target.position == newWorldPosition)
        {
            Debug.Log($"'{target.name}' is already centered on its children.");
            return;
        }

        Undo.SetCurrentGroupName("Center Pivot On Children");
        int group = Undo.GetCurrentGroup();

        // Snapshot every direct child's world position BEFORE moving the parent.
        int childCount = target.childCount;
        Transform[] children = new Transform[childCount];
        Vector3[] worldPositions = new Vector3[childCount];
        for (int i = 0; i < childCount; i++)
        {
            children[i] = target.GetChild(i);
            worldPositions[i] = children[i].position;
        }

        // Move the parent's own pivot to the bounds center.
        Undo.RecordObject(target, "Center Pivot On Children");
        target.position = newWorldPosition;

        // Restore each child's original world position so nothing visually shifts.
        for (int i = 0; i < childCount; i++)
        {
            Undo.RecordObject(children[i], "Center Pivot On Children");
            children[i].position = worldPositions[i];
        }

        Undo.CollapseUndoOperations(group);
    }

    [MenuItem("CONTEXT/Transform/Center Pivot On Children", true)]
    private static bool CenterPivotOnChildrenValidate(MenuCommand command)
    {
        Transform target = command.context as Transform;
        return target != null && target.childCount > 0;
    }

    // Recursively grows 'bounds' to encapsulate the visual/geometric extent of 't' and
    // everything under it. Priority per-node: Renderer > Collider/Collider2D >
    // RectTransform corners > (only if it's a leaf with none of the above) its own position.
    private static void EncapsulateHierarchy(Transform t, ref Bounds bounds, ref bool initialized)
    {
        Bounds? local = null;

        Renderer renderer = t.GetComponent<Renderer>();
        if (renderer != null)
        {
            local = renderer.bounds;
        }
        else
        {
            Collider collider = t.GetComponent<Collider>();
            if (collider != null)
            {
                local = collider.bounds;
            }
            else
            {
                Collider2D collider2D = t.GetComponent<Collider2D>();
                if (collider2D != null)
                {
                    local = collider2D.bounds;
                }
                else if (t is RectTransform rt)
                {
                    Vector3[] corners = new Vector3[4];
                    rt.GetWorldCorners(corners);
                    Bounds rectBounds = new Bounds(corners[0], Vector3.zero);
                    for (int i = 1; i < 4; i++)
                        rectBounds.Encapsulate(corners[i]);
                    local = rectBounds;
                }
                else if (t.childCount == 0)
                {
                    // Leaf with no visual/physical component — fall back to its position
                    // so it still contributes a point to the bounds.
                    local = new Bounds(t.position, Vector3.zero);
                }
            }
        }

        if (local.HasValue)
        {
            if (!initialized)
            {
                bounds = local.Value;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(local.Value);
            }
        }

        for (int i = 0; i < t.childCount; i++)
        {
            EncapsulateHierarchy(t.GetChild(i), ref bounds, ref initialized);
        }
    }
}