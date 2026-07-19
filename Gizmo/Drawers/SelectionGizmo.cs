using EditorEnhanced.Gizmo.Components;
using UnityEngine;

namespace EditorEnhanced.Gizmo.Drawers;

public static class SelectionGizmo
{
   public static GameObject SObject;

   public static GameObject Create()
   {
      if (SObject != null) return SObject;
      var anchor = new GameObject();
      anchor.name = "SelectionGizmo";
      anchor.layer = 22;
      anchor.transform.localScale = new Vector3(0.333f, 0.1f, 0.1f);
      anchor.SetActive(false);

      var mesh = GameObject.CreatePrimitive(PrimitiveType.Quad);
      Object.Destroy(mesh.GetComponent<MeshCollider>());
      mesh.name = "Mesh";
      mesh.GetComponent<Renderer>().sharedMaterial = GizmoAssets.DefaultMaterial;
      mesh.transform.localPosition = Vector3.back * 2.5f;
      mesh.transform.localRotation = Quaternion.Euler(90f, 45f, 0f);
      mesh.transform.SetParent(anchor.transform, false);

      var highlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
      Object.Destroy(highlight.GetComponent<MeshCollider>());
      highlight.name = "PermanentHighlight";
      highlight.GetComponent<Renderer>().sharedMaterial = GizmoAssets.OutlineMaterial;
      highlight.transform.localScale *= 1.5f;
      highlight.transform.SetParent(mesh.transform, false);

      mesh.AddComponent<GizmoMaterial>();
      anchor.AddComponent<GizmoSelection>();

      return anchor;
   }
}
