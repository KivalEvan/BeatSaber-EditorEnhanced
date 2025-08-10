using EditorEnhanced.Gizmo.Components;
using UnityEngine;

namespace EditorEnhanced.Gizmo.Drawers;

internal static class SphereGizmo
{
    public static GameObject SObject;

    public static GameObject Create(Material material)
    {
        if (SObject != null) return SObject;
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "DistributionGizmo";
        go.layer = 22;
        go.SetActive(false);
        go.GetComponent<Renderer>().material = material;

        var highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.Destroy(highlight.GetComponent<SphereCollider>());
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

        go.AddComponent<GizmoHighlight>();
        go.AddComponent<GizmoHighlightController>();
        go.AddComponent<GizmoNone>();

        return go;
    }
}