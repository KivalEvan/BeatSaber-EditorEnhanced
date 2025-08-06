using UnityEngine;

namespace EditorEnhanced.Gizmo.Drawers;

internal static class CubeGizmo
{
    public static GameObject SObject;

    public static GameObject Create(Material material)
    {
        if (SObject != null) return SObject;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "BaseGizmo";
        go.layer = 22;
        go.SetActive(false);
        go.GetComponent<Renderer>().material = material;
        
        var highlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(highlight.GetComponent<BoxCollider>());
        highlight.name = "Highlight";
        highlight.SetActive(false);
        highlight.GetComponent<Renderer>().material = GizmoAssets.OutlineMaterial;
        highlight.transform.localScale *= 1.5f;
        highlight.transform.SetParent(go.transform, false);

        // var lineRenderer = go.AddComponent<LineRenderer>();
        // lineRenderer.startWidth = 0.1f;
        // lineRenderer.endWidth = 0.1f;
        // lineRenderer.positionCount = 0;

        // var lineRenderController = go.AddComponent<LineRenderController>();
        // lineRenderController.enabled = false;

        go.AddComponent<GizmoHighlighter>();
        go.AddComponent<GizmoHighlighterGroup>();
        go.AddComponent<GizmoNone>();

        return go;
    }
}