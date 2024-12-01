using UnityEngine;

namespace EditorEnhanced.Gizmo;

internal class ColorGizmo : BaseGizmo
{
    public static GameObject SObject;

    public static GameObject Create(Material material)
    {
        var go = new GameObject();
        var prim = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        prim.GetComponent<Renderer>().material = material;
        prim.gameObject.transform.SetParent(go.transform, false);
        prim.transform.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
        prim.gameObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

        go.SetActive(false);
        return go;
    }
}