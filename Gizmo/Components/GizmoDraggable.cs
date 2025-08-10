using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using EditorEnhanced.Configuration;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace EditorEnhanced.Gizmo.Components;

public abstract class GizmoDraggable : MonoBehaviour, IGizmoInput
{
    public LightGroupSubsystem LightGroupSubsystemContext;
    public LightAxis Axis;
    public Transform TargetTransform;
    public bool Mirror;
    [Inject] protected readonly BeatmapState _beatmapState;
    [Inject] protected readonly PluginConfig _config;
    [Inject] protected readonly SignalBus _signalBus;
    protected Camera Camera;
    public EventBoxEditorData EventBoxEditorDataContext;
    protected Vector3 InitialScreenPosition;

    private void Awake()
    {
        Camera = Camera.main;
    }

    protected virtual void OnEnable()
    {
        AdjustSize();
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

    protected abstract float GetSize();

    protected void AdjustSize()
    {
        var localScale = transform.localScale;
        var lossyScale = transform.lossyScale;
        transform.localScale = new Vector3(
            localScale.x / lossyScale.x * GetSize() * _config.Gizmo.GlobalScale,
            localScale.y / lossyScale.y * GetSize() * _config.Gizmo.GlobalScale,
            localScale.z / lossyScale.z * GetSize() * _config.Gizmo.GlobalScale);
    }

    protected Vector3 GetScreenPosition()
    {
        return new Vector3(Mouse.current.position.x.value, Mouse.current.position.y.value,
            Camera.WorldToScreenPoint(transform.parent.position).z);
    }
}