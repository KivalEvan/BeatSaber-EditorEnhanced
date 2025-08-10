using EditorEnhanced.Gizmo.Components;
using EditorEnhanced.Utils;
using UnityEngine;

namespace EditorEnhanced.Gizmo.Drawers;

internal static class TranslationGizmo
{
    public static GameObject SObject;

    public static GameObject Create(Material material)
    {
        if (SObject != null) return SObject;
        var bundle = AssetLoader.LoadFromResource(nameof(EditorEnhanced) + ".model");
        var go = bundle.LoadAsset<GameObject>("Assets/translation.prefab");
        go.name = "TranslationGizmo";
        go.layer = 22;
        go.SetActive(false);
        go.GetComponent<Renderer>().material = material;

        go.AddComponent<GizmoHighlight>();
        go.AddComponent<GizmoHighlightController>();
        go.AddComponent<GizmoDraggableTranslation>();

        return go;
    }
}