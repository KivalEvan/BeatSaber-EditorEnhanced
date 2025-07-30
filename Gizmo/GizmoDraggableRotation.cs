using System;
using UnityEngine;

namespace EditorEnhanced.Gizmo;

public class GizmoDraggableRotation : GizmoDraggable
{
    private Vector3 _initialEuler;
    private float _angleOffset;

    public override void OnDrag()
    {
        var screenPosition = GetScreenPosition();
        var worldPosition = Camera.ScreenToWorldPoint(screenPosition);
        transform.parent.position = worldPosition;

        transform.position = TargetTransform.position;
        var vec3 = screenPosition - InitialScreenPosition;
        var angle = Mathf.Atan2(vec3.y, vec3.x) * Mathf.Rad2Deg;
        var deltaRotation =
            Axis switch
            {
                LightAxis.X => new Vector3(0f, 0f, -angle + _angleOffset),
                LightAxis.Y => new Vector3(0f, -angle + _angleOffset, 0f),
                LightAxis.Z => new Vector3(0f, 0f, 90f + -angle + _angleOffset),
                _ => throw new ArgumentOutOfRangeException()
            };
        transform.eulerAngles = _initialEuler + deltaRotation;
    }

    public override void OnBeginDrag()
    {
        IsDragging = true;
        transform.parent.SetParent(TargetTransform.parent, true);
        InitialScreenPosition = Camera.WorldToScreenPoint(transform.position);
        _initialEuler = transform.eulerAngles;
        var screenPosition = GetScreenPosition();
        _angleOffset = (Mathf.Atan2(transform.right.y, transform.right.x) -
                        Mathf.Atan2(screenPosition.y, screenPosition.x)) *
                       Mathf.Rad2Deg;
        Highlighter.AddOutline();
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
        Highlighter.RemoveOutline();
        IsDragging = false;
    }
}