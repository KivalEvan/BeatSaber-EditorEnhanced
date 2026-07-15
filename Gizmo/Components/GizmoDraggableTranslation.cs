using System;
using BeatmapEditor3D.Commands;
using EditorEnhanced.Commands;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EditorEnhanced.Gizmo.Components;

public class GizmoDraggableTranslation : GizmoDraggable
{
   private Vector3 _dragLocalDelta;
   private Plane _dragPlane;
   private LightTranslationGroup _lightTranslationGroup;
   private float _limit;
   private Vector3 _startLocalPosition;
   private Vector3 _startPointerPosition;

   protected override void OnEnable()
   {
      base.OnEnable();
      _lightTranslationGroup = (LightTranslationGroup)LightGroupSubsystemContext;
      _limit = Axis switch
      {
         LightAxis.X => Mirror
            ? _lightTranslationGroup.xTranslationLimits.x
            : _lightTranslationGroup.xTranslationLimits.y,
         LightAxis.Y => Mirror
            ? _lightTranslationGroup.yTranslationLimits.x
            : _lightTranslationGroup.yTranslationLimits.y,
         LightAxis.Z => Mirror
            ? _lightTranslationGroup.zTranslationLimits.x
            : _lightTranslationGroup.zTranslationLimits.y,
         _ => throw new ArgumentOutOfRangeException()
      };
   }

   protected override float GetSize()
   {
      return _config.Gizmo.SizeTranslation;
   }

   private float SnapPosition(float v, float scale)
   {
      var precision = _limit
         * scale
         / ModifyHoveredLightTranslationDeltaTranslationCommand._precisions
            [_beatmapState.scrollPrecision];
      return
         Mathf.Round(v / precision) * precision;
   }

   public override void OnDrag()
   {
      if (!IsDragging) return;
      var origin = transform.parent;
      var ray = Camera.ScreenPointToRay(Mouse.current.position.value);
      if (!_dragPlane.Raycast(ray, out var distance)) return;

      var localDelta = origin.parent.InverseTransformVector(ray.GetPoint(distance) - _startPointerPosition);
      _dragLocalDelta = Axis switch
      {
         LightAxis.X => new Vector3(
            SnapPosition(localDelta.x, TargetTransform.parent.lossyScale.x),
            0,
            0),
         LightAxis.Y => new Vector3(
            0,
            SnapPosition(localDelta.y, TargetTransform.parent.lossyScale.y),
            0),
         LightAxis.Z => new Vector3(
            0,
            0,
            SnapPosition(localDelta.z, TargetTransform.parent.lossyScale.z)),
         _ => throw new ArgumentOutOfRangeException()
      };
      origin.localPosition = _startLocalPosition + _dragLocalDelta;
   }

   public override void OnMouseClick()
   {
      if (!_config.Gizmo.Draggable) return;

      var origin = transform.parent;
      _dragPlane = new Plane(Camera.transform.forward, origin.position);
      var ray = Camera.ScreenPointToRay(Mouse.current.position.value);
      if (!_dragPlane.Raycast(ray, out var distance)) return;

      _startLocalPosition = origin.localPosition;
      _startPointerPosition = ray.GetPoint(distance);
      _dragLocalDelta = Vector3.zero;
      IsDragging = true;
   }

   public override void OnMouseRelease()
   {
      if (!IsDragging) return;

      if (_dragLocalDelta.sqrMagnitude > 0.001f)
      {
         var targetLocalPosition = TargetTransform.localPosition;
         var value = Axis switch
         {
            LightAxis.X => targetLocalPosition.x / _limit
               + _dragLocalDelta.x / _limit / TargetTransform.parent.lossyScale.x,
            LightAxis.Y => targetLocalPosition.y / _limit
               + _dragLocalDelta.y / _limit / TargetTransform.parent.lossyScale.y,
            LightAxis.Z => targetLocalPosition.z / _limit
               + _dragLocalDelta.z / _limit / TargetTransform.parent.lossyScale.z,
            _ => throw new ArgumentOutOfRangeException()
         };
         _signalBus.Fire(new DragGizmoLightTranslationEventBoxSignal(EventBoxEditorDataContext, value));
      }

      transform.parent.localPosition = _startLocalPosition;
      _dragLocalDelta = Vector3.zero;
      IsDragging = false;
   }
}
