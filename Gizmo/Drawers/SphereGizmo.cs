using UnityEngine;

namespace EditorEnhanced.Gizmo.Drawers;

internal static class SphereGizmo
{
    public static GameObject SObject;

    public static GameObject Create(Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.SetActive(false);
        go.GetComponent<Renderer>().material = material;
        go.gameObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        return go;
    }
}