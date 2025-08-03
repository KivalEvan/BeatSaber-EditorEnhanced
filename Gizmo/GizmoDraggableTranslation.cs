using System;
using BeatmapEditor3D.Commands;
using EditorEnhanced.Commands;
using UnityEngine;

namespace EditorEnhanced.Gizmo;

public class GizmoDraggableTranslation : GizmoDraggable
{
    private float SnapPosition(float v, float limit)
    {
        var precision = ModifyHoveredLightTranslationDeltaTranslationCommand._precisions
            [_beatmapState.scrollPrecision] / limit;
        return
            Mathf.Round(v / precision) * precision;
    }

    public override void OnDrag()
    {
        var screenPosition = GetScreenPosition();
        var worldPosition = Camera.ScreenToWorldPoint(screenPosition);
        transform.parent.position = worldPosition;

        if (LightGroupSubsystemContext != null && LightGroupSubsystemContext is LightTranslationGroup ltg)
            transform.parent.localPosition = Axis switch
            {
                LightAxis.X => new Vector3(SnapPosition(transform.parent.localPosition.x, ltg.xTranslationLimits.y),
                    TargetTransform.localPosition.y, TargetTransform.localPosition.z
                ),
                LightAxis.Y => new Vector3(
                    TargetTransform.localPosition.x,
                    SnapPosition(transform.parent.localPosition.y, ltg.yTranslationLimits.y),
                    TargetTransform.localPosition.z
                ),
                LightAxis.Z => new Vector3(
                    TargetTransform.localPosition.x, TargetTransform.localPosition.y,
                    SnapPosition(transform.parent.localPosition.z, ltg.zTranslationLimits.y)
                ),
                _ => throw new ArgumentOutOfRangeException()
            };
    }

    public override void OnMouseClick()
    {
        transform.parent.SetParent(TargetTransform.parent, true);
        InitialScreenPosition = Camera.WorldToScreenPoint(transform.position);
    }

    public override void OnMouseRelease()
    {
        if (LightGroupSubsystemContext != null && LightGroupSubsystemContext is LightTranslationGroup ltg)
        {
            var result = transform.parent.localPosition;
            var value = Axis switch
            {
                LightAxis.X => result.x / ltg.xTranslationLimits.y,
                LightAxis.Y => result.y / ltg.yTranslationLimits.y,
                LightAxis.Z => result.z / ltg.zTranslationLimits.y,
                _ => throw new ArgumentOutOfRangeException()
            };
            if (Mirror) value = -value;
            _signalBus.Fire(new DragGizmoLightTranslationEventBoxSignal(EventBoxEditorDataContext, value));
        }

        // transform.parent.localPosition = Vector3.zero;
        transform.parent.SetParent(TargetTransform, true);
        transform.parent.position = TargetTransform.position;
    }
}