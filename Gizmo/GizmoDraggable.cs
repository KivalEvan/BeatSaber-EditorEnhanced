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

    private const float GizmoModSize = 2.5f;
    
    private void Awake()
    {
        Camera = Camera.main;
    }

    private void OnEnable()
    {
        var localScale = transform.localScale;
        var lossyScale = transform.lossyScale;
        transform.localScale = new Vector3(localScale.x / lossyScale.x * GizmoModSize,
            localScale.y / lossyScale.y * GizmoModSize, localScale.z / lossyScale.z * GizmoModSize);
        transform.localRotation = Axis switch
        {
            LightAxis.X => Mirror ? Quaternion.Euler(0, 270, 0) : Quaternion.Euler(0, 90, 0),
            LightAxis.Y => Mirror ? Quaternion.Euler(90, 0, 0) : Quaternion.Euler(270, 0, 0),
            LightAxis.Z => Mirror ? Quaternion.Euler(180, 0, 0) : Quaternion.Euler(0, 0, 0),
            _ => Quaternion.identity
        };
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