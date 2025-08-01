using System;
using BeatmapEditor3D.Commands;
using UnityEngine;

namespace EditorEnhanced.Gizmo;

public class GizmoDraggableRotation : GizmoDraggable
{
    private Vector3 _initialEuler;
    private float _angleOffset;

    private float SnapRotation(float v)
    {
        var precision = ModifyHoveredLightRotationDeltaRotationCommand._precisions
            [beatmapState.scrollPrecision];
        return
            Mathf.Round(v / precision) * precision;
    }

    public override void OnDrag()
    {
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

    public override void OnBeginDrag()
    {
        transform.parent.SetParent(TargetTransform.parent, true);
        InitialScreenPosition = Camera.WorldToScreenPoint(transform.position);
        _initialEuler = transform.eulerAngles;
        var screenPosition = GetScreenPosition();
        _angleOffset = 0f;
    }

    public override void OnEndDrag()
    {
        if (LightGroupSubsystemContext != null && LightGroupSubsystemContext is LightRotationGroup lrg)
        {
            transform.eulerAngles = _initialEuler;
        }

        // transform.parent.localPosition = Vector3.zero;
        transform.parent.SetParent(TargetTransform, true);
        transform.parent.position = TargetTransform.position;
    }
}