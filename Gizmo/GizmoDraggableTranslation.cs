using System;
using EditorEnhanced.Commands;

namespace EditorEnhanced.Gizmo;

public class GizmoDraggableTranslation : GizmoDraggable
{
    public override void OnDrag()
    {
        var screenPosition = GetScreenPosition();
        var worldPosition = Camera.ScreenToWorldPoint(screenPosition);
        transform.parent.position = worldPosition;

        transform.parent.localPosition = Axis switch
        {
            LightAxis.X => transform.parent.localPosition with
            {
                y = TargetTransform.localPosition.y, z = TargetTransform.localPosition.z
            },
            LightAxis.Y => transform.parent.localPosition with
            {
                x = TargetTransform.localPosition.x, z = TargetTransform.localPosition.z
            },
            LightAxis.Z => transform.parent.localPosition with
            {
                x = TargetTransform.localPosition.x, y = TargetTransform.localPosition.y
            },
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public override void OnBeginDrag()
    {
        IsDragging = true;
        Highlighter.AddOutline();
        transform.parent.SetParent(TargetTransform.parent, true);
        InitialScreenPosition = Camera.WorldToScreenPoint(transform.position);
    }

    public override void OnEndDrag()
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
            SignalBus.Fire(new DragGizmoLightTranslationEventBoxSignal(EventBoxEditorDataContext, value));
        }

        // transform.parent.localPosition = Vector3.zero;
        transform.parent.SetParent(TargetTransform, true);
        transform.parent.position = TargetTransform.position;
        Highlighter.RemoveOutline();
        IsDragging = false;
    }
}