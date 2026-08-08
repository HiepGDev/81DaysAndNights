using UnityEngine;
using UnityEditor;

/// <summary>
/// Adds "Apply Scale" commands to the Transform component's context menu (the ⋮ button
/// in the Inspector, or right-click on the component header) — similar to Blender's
/// Ctrl+A > Apply > Scale.
///
/// Unity transforms don't have separate mesh/vertex data to bake a scale into the way
/// Blender objects do, so instead this pushes the scale value down the hierarchy:
///   - The object's own scale is reset to (1,1,1).
///   - If it's a RectTransform (a UI element), its own sizeDelta is grown to compensate,
///     so its rendered rect size doesn't change.
///   - Direct children have their localPosition and localScale compensated so their
///     world position/size doesn't change either.
///
/// Two commands are provided:
///   - "Apply Scale (This Object Only)": bakes this object's scale into itself + its
///     direct children. Matches Blender's single-object Apply Scale.
///   - "Apply Scale To Hierarchy": recursively does the above all the way down, so
///     every single Transform under (and including) this one ends up at scale (1,1,1).
///     Handy when you scaled a top-level UI panel and want the whole subtree flattened.
///
/// Install: put this file anywhere inside a folder named "Editor" in your project
/// (e.g. Assets/Editor/ApplyScaleTool.cs).
/// </summary>
public static class ApplyScaleTool
{
    [MenuItem("CONTEXT/Transform/Apply Scale (This Object Only)", false, 1000)]
    private static void ApplyScaleSingle(MenuCommand command)
    {
        Transform target = (Transform)command.context;

        if (target.localScale == Vector3.one)
        {
            Debug.Log($"'{target.name}' already has a scale of (1,1,1). Nothing to apply.");
            return;
        }

        Undo.SetCurrentGroupName("Apply Scale");
        int group = Undo.GetCurrentGroup();

        ApplyScaleToChildren(target);

        Undo.CollapseUndoOperations(group);
    }

    [MenuItem("CONTEXT/Transform/Apply Scale (This Object Only)", true)]
    private static bool ApplyScaleSingleValidate(MenuCommand command)
    {
        Transform target = command.context as Transform;
        return target != null && target.localScale != Vector3.one;
    }

    [MenuItem("CONTEXT/Transform/Apply Scale To Hierarchy", false, 1001)]
    private static void ApplyScaleHierarchy(MenuCommand command)
    {
        Transform target = (Transform)command.context;

        Undo.SetCurrentGroupName("Apply Scale To Hierarchy");
        int group = Undo.GetCurrentGroup();

        ApplyScaleRecursive(target);

        Undo.CollapseUndoOperations(group);
    }

    // Bakes 'parent's current local scale into itself (sizeDelta, if UI) and its direct
    // children (localPosition/localScale), then resets parent's own scale to (1,1,1).
    private static void ApplyScaleToChildren(Transform parent)
    {
        Vector3 scale = parent.localScale;
        if (scale == Vector3.one)
            return;

        // IMPORTANT: snapshot every child's localPosition/localScale FIRST, before we
        // touch anything on the parent. A RectTransform's localPosition is derived live
        // from anchoredPosition + an anchor reference point that depends on the parent's
        // rect size. If we resized the parent's sizeDelta first and only then read
        // child.localPosition, we'd read an already-shifted value and double up the
        // compensation — which shows up as children jumping to the wrong spot (easy to
        // mistake for their anchors having changed, even though anchorMin/anchorMax are
        // never touched by this script).
        int childCount = parent.childCount;
        Transform[] children = new Transform[childCount];
        Vector3[] childPositions = new Vector3[childCount];
        Vector3[] childScales = new Vector3[childCount];
        for (int i = 0; i < childCount; i++)
        {
            Transform child = parent.GetChild(i);
            children[i] = child;
            childPositions[i] = child.localPosition;
            childScales[i] = child.localScale;
        }

        // If this is a UI element, grow its own rect to match how big it currently
        // renders, so removing the localScale doesn't shrink it back down.
        RectTransform rt = parent as RectTransform;
        if (rt != null)
        {
            Undo.RecordObject(rt, "Apply Scale");

            bool stretchedX = !Mathf.Approximately(rt.anchorMin.x, rt.anchorMax.x);
            bool stretchedY = !Mathf.Approximately(rt.anchorMin.y, rt.anchorMax.y);

            Vector2 newSizeDelta = rt.sizeDelta;
            if (!stretchedX) newSizeDelta.x *= scale.x;
            if (!stretchedY) newSizeDelta.y *= scale.y;
            rt.sizeDelta = newSizeDelta;

            if (stretchedX || stretchedY)
            {
                Debug.LogWarning(
                    $"'{parent.name}' has stretched anchors on one axis, so its own rect " +
                    "size on that axis wasn't auto-compensated. Check it still looks right.",
                    parent);
            }
        }

        // Now apply the compensation using the ORIGINAL snapshot values, not live ones.
        for (int i = 0; i < childCount; i++)
        {
            Transform child = children[i];
            Undo.RecordObject(child, "Apply Scale");

            child.localPosition = Vector3.Scale(childPositions[i], scale);
            child.localScale = Vector3.Scale(childScales[i], scale);
        }

        Undo.RecordObject(parent, "Apply Scale");
        parent.localScale = Vector3.one;
    }

    // Recursively flattens scale all the way down the hierarchy.
    private static void ApplyScaleRecursive(Transform parent)
    {
        ApplyScaleToChildren(parent);

        for (int i = 0; i < parent.childCount; i++)
        {
            ApplyScaleRecursive(parent.GetChild(i));
        }
    }
}