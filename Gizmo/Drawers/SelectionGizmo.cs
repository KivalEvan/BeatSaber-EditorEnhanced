using UnityEngine;

namespace EditorEnhanced.Gizmo.Drawers;

internal static class SelectionGizmo
{
    public static GameObject SObject;

    public static GameObject Create(Material material)
    {
        if (SObject != null) return SObject;
        var anchor = new GameObject();
        anchor.name = "SelectionGizmo";
        anchor.layer = 22;
        anchor.transform.localScale = new Vector3(0.333f, 0.1f, 0.1f);
        anchor.SetActive(false);

        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.Destroy(go.GetComponent<MeshCollider>());
        go.name = "Mesh";
        go.GetComponent<Renderer>().material = material;
        go.transform.localPosition = Vector3.back * 2.5f;
        go.transform.localRotation = Quaternion.Euler(90f, 45f, 0f);
        go.transform.SetParent(anchor.transform, false);
        
        var highlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.Destroy(highlight.GetComponent<MeshCollider>());
        highlight.name = "PermanentHighlight";
        highlight.GetComponent<Renderer>().material = GizmoAssets.OutlineMaterial;
        highlight.transform.localScale *= 1.5f;
        highlight.transform.SetParent(go.transform, false);

        anchor.AddComponent<GizmoSelection>();

        return anchor;
    }
}