using EditorEnhanced.Gizmo.Components;
using UnityEngine;
using UnityEngine.Animations;

namespace EditorEnhanced.Gizmo.Drawers;

internal static class CubeGizmo
{
   public static GameObject SObject;

   public static GameObject Create(Material material)
   {
      if (SObject != null) return SObject;
      var go = new GameObject("BaseGizmo");
      go.SetActive(false);

      var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
      mesh.name = "Mesh";
      mesh.layer = 22;
      mesh.GetComponent<Renderer>().material = material;
      mesh.transform.SetParent(go.transform, false);

      var highlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
      Object.Destroy(highlight.GetComponent<BoxCollider>());
      highlight.name = "Highlight";
      highlight.SetActive(false);
      highlight.GetComponent<Renderer>().material = GizmoAssets.OutlineMaterial;
      highlight.transform.localScale *= 1.5f;
      highlight.transform.SetParent(mesh.transform, false);

      // var lineRenderer = go.AddComponent<LineRenderer>();
      // lineRenderer.startWidth = 0.1f;
      // lineRenderer.endWidth = 0.1f;
      // lineRenderer.positionCount = 0;

      // var lineRenderController = go.AddComponent<LineRenderController>();
      // lineRenderController.enabled = false;

      go.AddComponent<ParentConstraint>().constraintActive = true;
      mesh.AddComponent<GizmoHighlight>();
      mesh.AddComponent<GizmoHighlightController>();
      mesh.AddComponent<GizmoNone>();

      return go;
   }
}