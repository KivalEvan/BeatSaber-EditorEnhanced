using UnityEngine;

namespace EditorEnhanced.Gizmo.Drawers;

internal static class SphereGizmo
{
    public static GameObject SObject;

    public static GameObject Create(Material material)
    {
        if (SObject != null) return SObject;
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.SetActive(false);
        go.GetComponent<Renderer>().material = material;
        var lineRenderer = go.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = 0;
        
        var lineRenderController = go.AddComponent<LineRenderController>();
        lineRenderController.enabled = false;
        
        return go;
    }
}