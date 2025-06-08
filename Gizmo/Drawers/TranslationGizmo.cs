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
        go.layer = 22;
        go.SetActive(false);
        go.GetComponent<Renderer>().material = material;
        
        var gizmoDraggable = go.AddComponent<GizmoDraggable>();
        
        return go;
    }
}