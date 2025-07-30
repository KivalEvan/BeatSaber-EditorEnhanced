using System;
using System.Collections.Generic;
using BeatmapEditor3D;
using EditorEnhanced.Commands;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Zenject;

namespace EditorEnhanced.Gizmo;

public abstract class GizmoDraggable : MonoBehaviour, IGizmoInput
{
    public LightGroupSubsystem LightGroupSubsystemContext;
    public EventBoxEditorData EventBoxEditorDataContext;
    public LightAxis Axis;
    public SignalBus SignalBus;
    public Transform TargetTransform;
    public bool Mirror;

    public GizmoHighlighter Highlighter;
    
    protected Camera Camera;
    protected Vector3 InitialScreenPosition;
    protected bool IsDragging;

    private void Awake()
    {
        Camera = Camera.main;
        Highlighter = GetComponent<GizmoHighlighter>();
    }

    protected Vector3 GetScreenPosition()
    {
        return new Vector3(Mouse.current.position.x.value, Mouse.current.position.y.value,
            Camera.WorldToScreenPoint(transform.parent.position).z);
    }

    public void OnPointerEnter()
    {
        if (!IsDragging) Highlighter.AddOutline();
    }

    public void OnPointerExit()
    {
        if (!IsDragging) Highlighter.RemoveOutline();
    }

    public abstract void OnDrag();
    public abstract void OnBeginDrag();
    public abstract void OnEndDrag();
}