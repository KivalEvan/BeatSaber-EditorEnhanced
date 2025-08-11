using System;
using BeatmapEditor3D.Commands;
using EditorEnhanced.Gizmo.Commands;
using UnityEngine;

namespace EditorEnhanced.Gizmo.Components;

public class GizmoDraggableRotation : GizmoDraggable
{
    private Vector3 _initialEuler;

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
        var screenPosition = GetScreenPosition();
        var vec3 = screenPosition - InitialScreenPosition;
        var angle = Mathf.Atan2(vec3.y, vec3.x) * Mathf.Rad2Deg;
        var deltaRotation =
            Axis switch
            {
                LightAxis.X => new Vector3(0f, 0f, SnapRotation(angle + 90f)),
                LightAxis.Y => new Vector3(0f, SnapRotation(-angle - 180f), 0f),
                LightAxis.Z => new Vector3(0f, 0f, SnapRotation(-angle - 180f)),
                _ => throw new ArgumentOutOfRangeException()
            };
        transform.eulerAngles = _initialEuler + deltaRotation;
    }

    public override void OnMouseClick()
    {
        if (!_config.Gizmo.Draggable) return;
        transform.parent.SetParent(TargetTransform.parent, true);
        InitialScreenPosition = Camera.WorldToScreenPoint(transform.position);
        _initialEuler = transform.eulerAngles;
        IsDragging = true;
    }

    public override void OnMouseRelease()
    {
        if (!_config.Gizmo.Draggable && !IsDragging) return;
        if (LightGroupSubsystemContext != null && LightGroupSubsystemContext is LightRotationGroup lrg)
            transform.eulerAngles = _initialEuler;

        transform.parent.SetParent(TargetTransform, true);
        transform.parent.position = TargetTransform.position;
        IsDragging = false;
    }
}