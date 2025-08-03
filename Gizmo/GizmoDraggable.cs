using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace EditorEnhanced.Gizmo;

public abstract class GizmoDraggable : MonoBehaviour, IGizmoInput
{
    public LightGroupSubsystem LightGroupSubsystemContext;
    public LightAxis Axis;
    public Transform TargetTransform;
    public bool Mirror;
    [Inject] public BeatmapState beatmapState;

    protected Camera Camera;
    public EventBoxEditorData EventBoxEditorDataContext;
    protected Vector3 InitialScreenPosition;
    [Inject] public SignalBus SignalBus;

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
    public abstract void OnBeginDrag();
    public abstract void OnEndDrag();

    protected Vector3 GetScreenPosition()
    {
        return new Vector3(Mouse.current.position.x.value, Mouse.current.position.y.value,
            Camera.WorldToScreenPoint(transform.parent.position).z);
    }
}