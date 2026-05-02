using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class CenterPivotToBounds
{
    [MenuItem("Tools/Scene/Fix Selected Pivot To Bounds Center", true)]
    private static bool CanFixSelectedPivot()
    {
        return Selection.transforms.Length > 0;
    }

    [MenuItem("Tools/Scene/Fix Selected Pivot To Bounds Center")]
    private static void FixSelectedPivot()
    {
        var wrappedObjects = new List<Object>();

        foreach (var selected in Selection.transforms)
        {
            if (!TryGetRendererBounds(selected, out var bounds))
            {
                Debug.LogWarning($"Skipping '{selected.name}' because it has no renderers in its children.");
                continue;
            }

            var oldParent = selected.parent;
            var oldSiblingIndex = selected.GetSiblingIndex();
            var pivot = new GameObject($"{selected.name} Pivot");

            Undo.RegisterCreatedObjectUndo(pivot, "Create centered pivot");
            Undo.RecordObject(pivot.transform, "Position centered pivot");
            pivot.transform.SetParent(oldParent, false);
            pivot.transform.SetSiblingIndex(oldSiblingIndex);
            pivot.transform.position = bounds.center;
            pivot.transform.rotation = selected.rotation;
            pivot.transform.localScale = Vector3.one;

            Undo.SetTransformParent(selected, pivot.transform, "Move object under centered pivot");
            wrappedObjects.Add(pivot);
        }

        if (wrappedObjects.Count > 0)
        {
            Selection.objects = wrappedObjects.ToArray();
        }
    }

    private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        bounds = default;

        if (renderers.Length == 0)
        {
            return false;
        }

        bounds = renderers[0].bounds;

        for (var i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return true;
    }
}
