using System;
using System.Collections.Generic;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using EditorEnhanced.Commands;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Zenject;

namespace EditorEnhanced.Gizmo;

public abstract class GizmoDraggable : MonoBehaviour, IGizmoInput
{
    [Inject] public BeatmapState beatmapState;
    [Inject] public SignalBus SignalBus;
    
    public LightGroupSubsystem LightGroupSubsystemContext;
    public EventBoxEditorData EventBoxEditorDataContext;
    public LightAxis Axis;
    public Transform TargetTransform;
    public bool Mirror;
    
    protected Camera Camera;
    protected Vector3 InitialScreenPosition;

    private void Awake()
    {
        Camera = Camera.main;
    }

    protected Vector3 GetScreenPosition()
    {
        return new Vector3(Mouse.current.position.x.value, Mouse.current.position.y.value,
            Camera.WorldToScreenPoint(transform.parent.position).z);
    }

    public void OnPointerEnter()
    {
    }

    public void OnPointerExit()
    {
    }

    public abstract void OnDrag();
    public abstract void OnBeginDrag();
    public abstract void OnEndDrag();
}