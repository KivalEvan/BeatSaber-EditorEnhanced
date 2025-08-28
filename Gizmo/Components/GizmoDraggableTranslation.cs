using System;
using BeatmapEditor3D.Commands;
using EditorEnhanced.Commands;
using EditorEnhanced.Gizmo.Commands;
using UnityEngine;

namespace EditorEnhanced.Gizmo.Components;

public class GizmoDraggableTranslation : GizmoDraggable
{
   protected override void OnEnable()
   {
      base.OnEnable();
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

   private float SnapPosition(float v, float limit, float scale)
   {
      var precision = ModifyHoveredLightTranslationDeltaTranslationCommand._precisions
            [_beatmapState.scrollPrecision]
         / limit
         * scale;
      return
         Mathf.Round(v / precision) * precision;
   }

   public override void OnDrag()
   {
      if (!_config.Gizmo.Draggable && !IsDragging) return;
      var screenPosition = GetScreenPosition();
      var worldPosition = Camera.ScreenToWorldPoint(screenPosition);
      transform.parent.position = worldPosition;

      if (LightGroupSubsystemContext != null && LightGroupSubsystemContext is LightTranslationGroup ltg)
         transform.parent.localPosition = Axis switch
         {
            LightAxis.X => new Vector3(
               SnapPosition(
                  transform.parent.localPosition.x,
                  ltg.xTranslationLimits.y,
                  TargetTransform.parent.lossyScale.x),
               0,
               0
            ),
            LightAxis.Y => new Vector3(
               0,
               SnapPosition(
                  transform.parent.localPosition.y,
                  ltg.yTranslationLimits.y,
                  TargetTransform.parent.lossyScale.y),
               0
            ),
            LightAxis.Z => new Vector3(
               0,
               0,
               SnapPosition(
                  transform.parent.localPosition.z,
                  ltg.zTranslationLimits.y,
                  TargetTransform.parent.lossyScale.z)
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
      if (LightGroupSubsystemContext != null && LightGroupSubsystemContext is LightTranslationGroup ltg)
      {
         var localPosition = transform.parent.localPosition;
         var targetLocalPosition = TargetTransform.localPosition;
         var value = Axis switch
         {
            LightAxis.X => targetLocalPosition.x / ltg.xTranslationLimits.y
               + localPosition.x / ltg.xTranslationLimits.y * TargetTransform.parent.lossyScale.x,
            LightAxis.Y => targetLocalPosition.y / ltg.yTranslationLimits.y
               + localPosition.y / ltg.yTranslationLimits.y * TargetTransform.parent.lossyScale.y,
            LightAxis.Z => targetLocalPosition.z / ltg.zTranslationLimits.y
               + localPosition.z / ltg.zTranslationLimits.y * TargetTransform.parent.lossyScale.z,
            _ => throw new ArgumentOutOfRangeException()
         };
         if (Mirror) value = -value;
         _signalBus.Fire(new DragGizmoLightTranslationEventBoxSignal(EventBoxEditorDataContext, value));
      }

      transform.parent.localPosition = Vector3.zero;
      IsDragging = false;
   }
}