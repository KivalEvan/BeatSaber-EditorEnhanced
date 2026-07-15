using BeatmapEditor3D.Commands;
using EditorEnhanced.Commands;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EditorEnhanced.Gizmo.Components;

public class GizmoDraggableRotation : GizmoDraggable
{
   protected override float GetSize()
   {
      return _config.Gizmo.SizeRotation;
   }

   private float SnapRotation(float v)
   {
      var precision = ModifyHoveredLightRotationDeltaRotationCommand._precisions
         [_beatmapState.scrollPrecision];
      return
         Mathf.Round(v / precision) * precision;
   }

   public override void OnDrag()
   {
      if (!_config.Gizmo.Draggable && !IsDragging) return;
      var origin = transform.parent;
      var axisNormal = GizmoRotationMath.GetAxisNormal(origin.parent, Axis);

      var ray = Camera.ScreenPointToRay(Mouse.current.position.value);
      var rotationPlane = new Plane(axisNormal, origin.position);

      if (!rotationPlane.Raycast(ray, out var distance)) return;
      var hitPoint = ray.GetPoint(distance);
      var direction = hitPoint - origin.position;

      var targetRotation = Quaternion.LookRotation(direction, axisNormal);
      origin.rotation = targetRotation;
      origin.localEulerAngles = GizmoRotationMath.GetSnappedLocalEuler(origin.localEulerAngles, Axis, SnapRotation);
   }

   public override void OnMouseClick()
   {
      if (!_config.Gizmo.Draggable) return;
      IsDragging = true;
   }

   public override void OnMouseRelease()
   {
      if (!_config.Gizmo.Draggable && !IsDragging) return;
      var localRotation = transform.parent.localEulerAngles;
      var targetLocalRotation = TargetTransform.localEulerAngles;
      var value = GizmoRotationMath.GetTargetValue(localRotation, targetLocalRotation, Axis, Mirror);
      _signalBus.Fire(new DragGizmoLightRotationEventBoxSignal(EventBoxEditorDataContext, Mathf.Repeat(value, 360f)));

      transform.parent.localRotation = Quaternion.identity;
      IsDragging = false;
   }
}
