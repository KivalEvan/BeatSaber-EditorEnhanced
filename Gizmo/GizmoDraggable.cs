using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace EditorEnhanced.Gizmo;

public abstract class GizmoDraggable : MonoBehaviour, IGizmoInput
{
    public EventBoxEditorData EventBoxEditorDataContext;
    public LightGroupSubsystem LightGroupSubsystemContext;
    public LightAxis Axis;
    public Transform TargetTransform;
    public bool Mirror;
    
    protected Camera Camera;
    protected Vector3 InitialScreenPosition;

    [Inject] protected readonly BeatmapState _beatmapState;
    [Inject] protected readonly SignalBus _signalBus;

    private void Awake()
    {
        Camera = Camera.main;
    }

    public void OnPointerEnter()
    {
    }

    public void OnPointerExit()
    {
    }

    public abstract void OnDrag();
    public abstract void OnMouseClick();
    public abstract void OnMouseRelease();

    protected Vector3 GetScreenPosition()
    {
        return new Vector3(Mouse.current.position.x.value, Mouse.current.position.y.value,
            Camera.WorldToScreenPoint(transform.parent.position).z);
    }
}