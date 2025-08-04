using UnityEngine;

namespace EditorEnhanced.Gizmo.Drawers;

internal static class LaneGizmo
{
    public static GameObject SObject;

    public static GameObject Create(Material material)
    {
        if (SObject != null) return SObject;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.layer = 22;
        go.transform.localScale = new Vector3(0.333f, 0.1f, 0.1f);
        go.SetActive(false);
        go.GetComponent<Renderer>().material = material;
        
        var highlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(highlight.GetComponent<BoxCollider>());
        highlight.name = "Highlight";
        highlight.SetActive(false);
        highlight.GetComponent<Renderer>().material = GizmoAssets.OutlineMaterial;
        highlight.transform.localScale *= 1.5f;
        highlight.transform.SetParent(go.transform, false);

        var selection = GameObject.CreatePrimitive(PrimitiveType.Quad);
        selection.name = "Selection";
        selection.SetActive(false);
        selection.GetComponent<Renderer>().material = GizmoAssets.DefaultMaterial;
        selection.transform.localPosition = Vector3.back * 2.5f;
        selection.transform.localRotation = Quaternion.Euler(90f, 45f, 0f);
        selection.transform.SetParent(go.transform, false);
        
        var selectionHighlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
        selectionHighlight.name = "Highlight";
        selectionHighlight.GetComponent<Renderer>().material = GizmoAssets.OutlineMaterial;
        selectionHighlight.transform.localScale *= 1.5f;
        selectionHighlight.transform.SetParent(selection.transform, false);

        // var lineRenderer = go.AddComponent<LineRenderer>();
        // lineRenderer.startWidth = 0.1f;
        // lineRenderer.endWidth = 0.1f;
        // lineRenderer.positionCount = 0;

        // var lineRenderController = go.AddComponent<LineRenderController>();
        // lineRenderController.enabled = false;

        go.AddComponent<GizmoHighlighter>();
        go.AddComponent<GizmoHighlighterGroup>();
        go.AddComponent<GizmoSwappable>();

        return go;
    }
}