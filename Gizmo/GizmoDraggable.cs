using System;
using System.Collections.Generic;
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

    private Vector3 _initialEuler;
    private Vector3 _initialScreenPosition;
    private float _angleOffset;
    private bool _isDragging;

    public void OnMouseDrag()
    {
        var screenPosition = GetScreenPosition();
        var worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
        transform.parent.position = worldPosition;

        if (LightGroupSubsystemContext is not LightTranslationGroup ltg)
        {
            transform.parent.localPosition = Vector3.zero;
            var vec3 = screenPosition - _initialScreenPosition;
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
            return;
        }

        transform.parent.localPosition = Axis switch
        {
            LightAxis.X => transform.parent.localPosition with { y = 0, z = 0 },
            LightAxis.Y => transform.parent.localPosition with { x = 0, z = 0 },
            LightAxis.Z => transform.parent.localPosition with { x = 0, y = 0 },
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public void OnMouseDown()
    {
        _initialScreenPosition = Camera.main.WorldToScreenPoint(transform.position);
        _initialEuler = transform.eulerAngles;
        var screenPosition = GetScreenPosition();
        _angleOffset = (Mathf.Atan2(transform.right.y, transform.right.x) -
                        Mathf.Atan2(screenPosition.y, screenPosition.x)) *
                       Mathf.Rad2Deg;
        _isDragging = true;
        AddOutline();
    }

    public void OnMouseUp()
    {
        if (LightGroupSubsystemContext != null && LightGroupSubsystemContext is LightRotationGroup lrg)
        {
            transform.eulerAngles = _initialEuler;
        }

        if (LightGroupSubsystemContext != null && LightGroupSubsystemContext is LightTranslationGroup ltg)
        {
            var result = transform.parent.parent.localPosition +
                         transform.parent.localPosition;
            var value = Axis switch
            {
                LightAxis.X => result.x / ltg.xTranslationLimits.y,
                LightAxis.Y => result.y / ltg.yTranslationLimits.y,
                LightAxis.Z => result.z / ltg.zTranslationLimits.y,
                _ => throw new ArgumentOutOfRangeException()
            };
            SignalBus.Fire(new DragGizmoLightTranslationEventBoxSignal(EventBoxEditorDataContext, value));
        }

        transform.parent.localPosition = Vector3.zero;
        RemoveOutline();
        _isDragging = false;
    }

    public void OnMouseEnter()
    {
        if (!_isDragging) AddOutline();
    }

    public void OnMouseExit()
    {
        if (!_isDragging) RemoveOutline();
    }

    private Vector3 GetScreenPosition()
    {
        return new Vector3(Mouse.current.position.x.value, Mouse.current.position.y.value,
            Camera.main.WorldToScreenPoint(transform.parent.position).z);
    }

    private void AddOutline()
    {
        var renderer = gameObject.GetComponent<Renderer>();
        var mats = new List<Material>();
        renderer.GetSharedMaterials(mats);
        if (!mats.Contains(GizmoAssets.OutlineMaterial)) mats.Insert(0, GizmoAssets.OutlineMaterial);
        renderer.SetSharedMaterials(mats);
    }

    private void RemoveOutline()
    {
        var renderer = gameObject.GetComponent<Renderer>();
        var mats = new List<Material>();
        renderer.GetSharedMaterials(mats);
        mats.Remove(GizmoAssets.OutlineMaterial);
        renderer.SetSharedMaterials(mats);
    }
}