using System;
using BeatmapEditor3D.Commands;
using EditorEnhanced.Commands;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EditorEnhanced.Gizmo.Components;

public class GizmoDraggableRotation : GizmoDraggable
{
   private float _dragAngle;
   private Plane _dragPlane;
   private Vector3 _startDirection;
   private Quaternion _startLocalRotation;
   private float _startValue;

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
      if (!IsDragging) return;
      var origin = transform.parent;
      var axisNormal = GizmoRotationMath.GetAxisNormal(origin.parent, Axis);

      var ray = Camera.ScreenPointToRay(Mouse.current.position.value);
      if (!_dragPlane.Raycast(ray, out var distance)) return;

      var direction = ray.GetPoint(distance) - origin.position;
      if (direction.sqrMagnitude <= Mathf.Epsilon) return;

      _dragAngle = SnapRotation(Vector3.SignedAngle(_startDirection, direction, axisNormal));
      var localAxis = Axis switch
      {
         LightAxis.X => Vector3.right,
         LightAxis.Y => Vector3.up,
         LightAxis.Z => Vector3.forward,
         _ => throw new ArgumentOutOfRangeException()
      };
      origin.localRotation = Quaternion.AngleAxis(_dragAngle, localAxis) * _startLocalRotation;
   }

   public override void OnMouseClick()
   {
      if (!_config.Gizmo.Draggable) return;

      var origin = transform.parent;
      var axisNormal = GizmoRotationMath.GetAxisNormal(origin.parent, Axis);
      _dragPlane = new Plane(axisNormal, origin.position);
      var ray = Camera.ScreenPointToRay(Mouse.current.position.value);
      if (!_dragPlane.Raycast(ray, out var distance)) return;

      _startDirection = ray.GetPoint(distance) - origin.position;
      if (_startDirection.sqrMagnitude <= Mathf.Epsilon) return;

      _startLocalRotation = origin.localRotation;
      _startValue = GizmoRotationMath.GetEventValue(TargetTransform.localRotation, Axis, Mirror);
      _dragAngle = 0f;
      IsDragging = true;
   }

   public override void OnMouseRelease()
   {
      if (!IsDragging) return;

      if (!Mathf.Approximately(_dragAngle, 0f))
      {
         var value = _startValue + (Mirror ? -_dragAngle : _dragAngle);
         _signalBus.Fire(
            new DragGizmoLightRotationEventBoxSignal(EventBoxEditorDataContext, Mathf.Repeat(value, 360f)));
      }

      transform.parent.localRotation = _startLocalRotation;
      _dragAngle = 0f;
      IsDragging = false;
   }
}
