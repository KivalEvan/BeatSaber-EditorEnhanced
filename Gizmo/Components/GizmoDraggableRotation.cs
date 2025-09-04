using System;
using BeatmapEditor3D.Commands;
using EditorEnhanced.Commands;
using EditorEnhanced.Gizmo.Commands;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EditorEnhanced.Gizmo.Components;

public class GizmoDraggableRotation : GizmoDraggable
{
   private Vector3 _axisNormal;
   private Transform _origin;

   protected override void OnEnable()
   {
      base.OnEnable();
      _signalBus.Subscribe<GizmoConfigSizeRotationUpdateSignal>(AdjustSize);
   }

   private void OnDisable()
   {
      _signalBus.TryUnsubscribe<GizmoConfigSizeRotationUpdateSignal>(AdjustSize);
   }

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
      var ray = Camera.ScreenPointToRay(Mouse.current.position.value);
      var rotationPlane = new Plane(_axisNormal, _origin.position);

      if (!rotationPlane.Raycast(ray, out var distance)) return;
      var hitPoint = ray.GetPoint(distance);
      var direction = hitPoint - _origin.position;

      var targetRotation = Quaternion.LookRotation(direction, _axisNormal);
      _origin.rotation = targetRotation;
      // dear god i dont know how to actually deal with rotation, let alone quaternion
      _origin.localEulerAngles = Axis switch
      {
         LightAxis.X => Mathf.Approximately(_origin.localEulerAngles.z, 90f)
            ? new Vector3(SnapRotation(-_origin.localEulerAngles.x + 270f), 0f, 0f)
            : new Vector3(SnapRotation(_origin.localEulerAngles.x) + 90f, 0f, 0f),
         LightAxis.Y => new Vector3(0f, SnapRotation(_origin.localEulerAngles.y), 0f),
         LightAxis.Z => Mathf.Approximately(_origin.localEulerAngles.z, 90f)
            ? new Vector3(0f, 0f, SnapRotation(-_origin.localEulerAngles.x))
            : new Vector3(0f, 0f, SnapRotation(_origin.localEulerAngles.x + 180f)),
         _ => throw new ArgumentOutOfRangeException()
      };
   }

   public override void OnMouseClick()
   {
      if (!_config.Gizmo.Draggable) return;
      InitialScreenPosition = Camera.WorldToScreenPoint(transform.parent.position);
      _origin = transform.parent;
      _axisNormal = Axis switch
      {
         LightAxis.X => _origin.right,
         LightAxis.Y => _origin.up,
         LightAxis.Z => _origin.forward,
         _ => throw new ArgumentOutOfRangeException()
      };
      IsDragging = true;
   }

   public override void OnMouseRelease()
   {
      if (!_config.Gizmo.Draggable && !IsDragging) return;
      if (LightGroupSubsystemContext != null)
      {
         var localRotation = transform.parent.localEulerAngles;
         var targetLocalRotation = TargetTransform.localEulerAngles;
         var value = Axis switch
         {
            LightAxis.X => localRotation.x + targetLocalRotation.x,
            LightAxis.Y => localRotation.y + targetLocalRotation.y,
            LightAxis.Z => localRotation.z + targetLocalRotation.z,
            _ => throw new ArgumentOutOfRangeException()
         };
         if (Mirror) value = -value;
         _signalBus.Fire(new DragGizmoLightRotationEventBoxSignal(EventBoxEditorDataContext, Mathf.Repeat(value, 360f)));
      }

      transform.parent.localRotation = Quaternion.identity;
      IsDragging = false;
   }
}