using EditorEnhanced.Configuration;
using EditorEnhanced.Gizmo.Commands;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.Gizmo.Components;

public class GizmoNone : MonoBehaviour
{
    public Transform TargetTransform;
    [Inject] private readonly PluginConfig _config;
    [Inject] private readonly SignalBus _signalBus;

    private void OnEnable()
    {
        transform.SetParent(TargetTransform.parent, false);
        transform.position = TargetTransform.position;
        transform.rotation = TargetTransform.parent.rotation;
        transform.SetParent(TargetTransform, true);
        UpdateSizeValue();

        _signalBus.Subscribe<GizmoConfigSizeBaseUpdateSignal>(UpdateSizeValue);
        _signalBus.Subscribe<GizmoConfigGlobalScaleUpdateSignal>(UpdateSizeValue);
    }

    private void OnDisable()
    {
        _signalBus.TryUnsubscribe<GizmoConfigSizeBaseUpdateSignal>(UpdateSizeValue);
        _signalBus.TryUnsubscribe<GizmoConfigGlobalScaleUpdateSignal>(UpdateSizeValue);
    }

    private void UpdateSizeValue()
    {
        AdjustSize();
        _signalBus.Fire<GizmoConfigSizeRotationUpdateSignal>();
        _signalBus.Fire<GizmoConfigSizeTranslationUpdateSignal>();
    }

    private void AdjustSize()
    {
        var localScale = transform.localScale;
        var lossyScale = transform.lossyScale;
        transform.localScale = new Vector3(
            Mathf.Abs(localScale.x / (lossyScale.x != 0 ? lossyScale.x : 1) * _config.Gizmo.SizeBase *
                      _config.Gizmo.GlobalScale),
            Mathf.Abs(localScale.y / (lossyScale.y != 0 ? lossyScale.y : 1) * _config.Gizmo.SizeBase *
                      _config.Gizmo.GlobalScale),
            Mathf.Abs(localScale.z / (lossyScale.z != 0 ? lossyScale.z : 1) * _config.Gizmo.SizeBase *
                      _config.Gizmo.GlobalScale));
    }
}