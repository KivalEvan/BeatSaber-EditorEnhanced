using UnityEngine;

namespace EditorEnhanced.Gizmo.Drawers;

public class GroupGizmo
{
    public static GameObject SObject;

    public static GameObject Create(Material material)
    {
        if (SObject != null) return SObject;
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.layer = 22;
        go.SetActive(false);
        go.GetComponent<Renderer>().material = material;
        go.AddComponent<GizmoHighlighter>();
        go.AddComponent<GizmoHighlighterGrouped>();
        
        return go;
    }
}