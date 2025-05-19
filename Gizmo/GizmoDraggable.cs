using System;
using BeatmapEditor3D;
using BeatmapEditor3D.Controller;
using BeatmapEditor3D.DataModels;
using EditorEnhanced.Commands;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Windows;
using Zenject;

namespace EditorEnhanced.Gizmo;

public class GizmoDraggable : MonoBehaviour
{
    public LightGroupSubsystem LightGroupSubsystemContext;
    public EventBoxEditorData EventBoxEditorDataContext;
    public LightAxis Axis;
    public SignalBus SignalBus;

    public void OnMouseDrag()
    {
        var screenPosition = new Vector3(Mouse.current.position.x.value, Mouse.current.position.y.value,
            Camera.main.WorldToScreenPoint(transform.position).z);
        var worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
        transform.position = worldPosition;
        transform.localPosition = Axis switch
        {
            LightAxis.X => transform.localPosition with { y = 0, z = 0 },
            LightAxis.Y => transform.localPosition with { x = 0, z = 0 },
            LightAxis.Z => transform.localPosition with { x = 0, y = 0 },
            _ => throw new ArgumentOutOfRangeException()
        };
        Plugin.Log.Info($"{Axis} {worldPosition} {transform.localPosition}");
    }

    public void OnMouseUp()
    {
        if (LightGroupSubsystemContext == null || LightGroupSubsystemContext is not LightTranslationGroup ltg) return;
        var result = transform.parent.parent.localPosition + transform.localPosition * transform.parent.localScale.x;
        transform.localPosition = Vector3.zero;
        var value = Axis switch
        {
            LightAxis.X => result.x / ltg.xTranslationLimits.y,
            LightAxis.Y => result.y / ltg.xTranslationLimits.y,
            LightAxis.Z => result.z / ltg.xTranslationLimits.y,
            _ => throw new ArgumentOutOfRangeException()
        };
        SignalBus.Fire(new DragGizmoLightTranslationEventBoxSignal(EventBoxEditorDataContext, value));
    }
}