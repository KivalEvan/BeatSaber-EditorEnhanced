using System;
using BeatmapEditor3D.Commands;
using EditorEnhanced.Commands;
using EditorEnhanced.Gizmo.Commands;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EditorEnhanced.Gizmo.Components;

public class GizmoDraggableTranslation : GizmoDraggable
{
   private LightTranslationGroup _lightTranslationGroup;
   private float _limit;

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
      _signalBus.Subscribe<GizmoConfigSizeTranslationUpdateSignal>(AdjustSize);
   }

   private void OnDisable()
   {
      _signalBus.TryUnsubscribe<GizmoConfigSizeTranslationUpdateSignal>(AdjustSize);
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
      if (!_config.Gizmo.Draggable && !IsDragging) return;
      var origin = transform.parent;
      var screenPosition = new Vector3(
         Mouse.current.position.x.value,
         Mouse.current.position.y.value,
         Camera.WorldToScreenPoint(origin.position).z);
      var worldPosition = Camera.ScreenToWorldPoint(screenPosition);
      origin.position = worldPosition;
      transform.parent.localPosition = Axis switch
      {
         LightAxis.X => new Vector3(
            SnapPosition(origin.localPosition.x, TargetTransform.parent.lossyScale.x),
            0,
            0
         ),
         LightAxis.Y => new Vector3(
            0,
            SnapPosition(origin.localPosition.y, TargetTransform.parent.lossyScale.y),
            0
         ),
         LightAxis.Z => new Vector3(
            0,
            0,
            SnapPosition(origin.localPosition.z, TargetTransform.parent.lossyScale.z)
         ),
         _ => throw new ArgumentOutOfRangeException()
      };
   }

   public override void OnMouseClick()
   {
      if (!_config.Gizmo.Draggable) return;
      IsDragging = true;
   }

   public override void OnMouseRelease()
   {
      if (!_config.Gizmo.Draggable && !IsDragging) return;

      if (transform.parent.localPosition.sqrMagnitude > 0.001f)
      {
         var localPosition = transform.parent.localPosition;
         var targetLocalPosition = TargetTransform.localPosition;
         var value = Axis switch
         {
            LightAxis.X => targetLocalPosition.x / _limit
               + localPosition.x / _limit / TargetTransform.parent.lossyScale.x,
            LightAxis.Y => targetLocalPosition.y / _limit
               + localPosition.y / _limit / TargetTransform.parent.lossyScale.y,
            LightAxis.Z => targetLocalPosition.z / _limit
               + localPosition.z / _limit / TargetTransform.parent.lossyScale.z,
            _ => throw new ArgumentOutOfRangeException()
         };
         _signalBus.Fire(new DragGizmoLightTranslationEventBoxSignal(EventBoxEditorDataContext, value));
      }

      transform.parent.localPosition = Vector3.zero;
      IsDragging = false;
   }
}