using EditorEnhanced.Gizmo.Components;
using UnityEngine;
using UnityEngine.Animations;

namespace EditorEnhanced.Gizmo;

public sealed class GizmoInstance
{
   public GizmoInstance(GizmoType type, GameObject gameObject)
   {
      Type = type;
      GameObject = gameObject;
      Transform = gameObject.transform;
      Material = gameObject.GetComponent<GizmoMaterial>();
      Swappable = gameObject.GetComponent<GizmoSwappable>();
      LaneScrollable = gameObject.GetComponent<GizmoLaneScrollable>();
      HighlightController = gameObject.GetComponentInChildren<GizmoHighlightController>(true);
      Highlight = gameObject.GetComponentInChildren<GizmoHighlight>(true);
      Draggable = gameObject.GetComponentInChildren<GizmoDraggable>(true);
      ScaleController = gameObject.GetComponentInChildren<GizmoScaleController>(true);
      PositionConstraints = gameObject.GetComponentsInChildren<PositionConstraint>(true);
      RotationConstraints = gameObject.GetComponentsInChildren<RotationConstraint>(true);
      Poolables = gameObject.GetComponentsInChildren<IGizmoPoolable>(true);
   }

   public GizmoType Type { get; }
   public GameObject GameObject { get; }
   public Transform Transform { get; }
   public GizmoMaterial Material { get; }
   public GizmoSwappable Swappable { get; }
   public GizmoLaneScrollable LaneScrollable { get; }
   public GizmoHighlightController HighlightController { get; }
   public GizmoHighlight Highlight { get; }
   public GizmoDraggable Draggable { get; }
   public GizmoScaleController ScaleController { get; }
   public PositionConstraint[] PositionConstraints { get; }
   public RotationConstraint[] RotationConstraints { get; }
   public PositionConstraint PositionConstraint => PositionConstraints[0];
   public RotationConstraint RotationConstraint => RotationConstraints[0];
   public IGizmoPoolable[] Poolables { get; }
}
