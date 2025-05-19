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
        go.SetActive(false);
        go.GetComponent<Renderer>().material = material;
        
        var gizmoDraggable = go.AddComponent<GizmoDraggable>();

        go.transform.localScale = new Vector3(5f, 5f, 5f);
        return go;
    }
}