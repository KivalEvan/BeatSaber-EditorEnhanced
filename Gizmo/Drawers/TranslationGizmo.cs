using UnityEngine;

namespace EditorEnhanced.Gizmo;

internal class TranslationGizmo : BaseGizmo
{
    public static GameObject SObject;

    public static GameObject Create(Material material)
    {
        var go = new GameObject();
        var prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prim.GetComponent<Renderer>().material = material;
        prim.gameObject.transform.SetParent(go.transform, false);
        prim.transform.transform.localPosition = new Vector3(0.0f, 0.0f, 1.0f);
        prim.gameObject.transform.localScale = new Vector3(0.05f, 0.05f, 2.0f);

        prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prim.GetComponent<Renderer>().material = material;
        prim.gameObject.transform.SetParent(go.transform, false);
        prim.gameObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        prim.transform.transform.localPosition = new Vector3(0.0f, 0.0f, 2.0f);

        go.SetActive(false);
        return go;
    }
}