using UnityEngine;

namespace EditorEnhanced.Gizmo.Drawers;

internal static class CubeGizmo
{
    public static GameObject SObject;

    public static GameObject Create(Material material)
    {
        if (SObject != null) return SObject;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.SetActive(false);
        go.GetComponent<Renderer>().material = material;
        var lineRenderer = go.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        
        var gizmoDraggable = go.AddComponent<GizmoDraggable>();
        
        var lineRenderController = go.AddComponent<LineRenderController>();
        lineRenderController.enabled = false;
        go.gameObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        return go;
    }
}