using EditorEnhanced.Utils;
using UnityEngine;

namespace EditorEnhanced.Gizmo;

internal static class TranslationGizmo
{
    public static GameObject SObject;

    public static GameObject Create(Material material)
    {
        var bundle = AssetLoader.LoadFromResource(nameof(EditorEnhanced) + ".model");
        var go = bundle.LoadAsset<GameObject>("Assets/translation.prefab");
        go.SetActive(false);
        go.GetComponent<Renderer>().material = material;
        go.transform.localScale = new Vector3(5f, 5f, 5f);
        return go;
    }
}