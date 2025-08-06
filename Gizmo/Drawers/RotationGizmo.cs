using EditorEnhanced.Utils;
using UnityEngine;

namespace EditorEnhanced.Gizmo.Drawers;

internal static class RotationGizmo
{
    public static GameObject SObject;

    public static GameObject Create(Material material)
    {
        if (SObject != null) return SObject;
        var bundle = AssetLoader.LoadFromResource(nameof(EditorEnhanced) + ".model");
        var go = bundle.LoadAsset<GameObject>("Assets/rotation.prefab");
        go.name = "RotationGizmo";
        go.layer = 22;
        go.SetActive(false);
        go.GetComponent<Renderer>().material = material;

        go.AddComponent<GizmoHighlighter>();
        go.AddComponent<GizmoHighlighterGroup>();
        go.AddComponent<GizmoDraggableRotation>();

        return go;
    }
}