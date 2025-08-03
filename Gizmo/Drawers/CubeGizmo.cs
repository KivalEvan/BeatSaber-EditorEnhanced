using UnityEngine;

namespace EditorEnhanced.Gizmo.Drawers;

internal static class CubeGizmo
{
    public static GameObject SObject;

    public static GameObject Create(Material material)
    {
        if (SObject != null) return SObject;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.layer = 22;
        go.SetActive(false);
        go.GetComponent<Renderer>().material = material;
        // var lineRenderer = go.AddComponent<LineRenderer>();
        // lineRenderer.startWidth = 0.1f;
        // lineRenderer.endWidth = 0.1f;
        // lineRenderer.positionCount = 0;

        // var lineRenderController = go.AddComponent<LineRenderController>();
        // lineRenderController.enabled = false;

        go.AddComponent<GizmoHighlighter>();
        go.AddComponent<GizmoHighlighterGroup>();

        return go;
    }
}