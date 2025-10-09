using EditorEnhanced.Gizmo.Components;
using EditorEnhanced.Utils;
using UnityEngine;

namespace EditorEnhanced.Gizmo.Drawers;

internal static class RotationGizmo
{
   public static GameObject SObject;

   public static GameObject Create()
   {
      if (SObject != null) return SObject;
      var bundle = AssetLoader.LoadFromResource(nameof(EditorEnhanced) + ".model");
      var go = bundle.LoadAsset<GameObject>("Assets/rotation.prefab");
      go.name = "RotationGizmo";
      go.layer = 22;
      go.SetActive(false);

      go.AddComponent<GizmoMaterial>();
      go.AddComponent<GizmoHighlight>();
      go.AddComponent<GizmoHighlightController>();
      go.AddComponent<GizmoDraggableRotation>();

      return go;
   }
}