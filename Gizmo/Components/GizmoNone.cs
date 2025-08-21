using EditorEnhanced.Configuration;
using EditorEnhanced.Gizmo.Commands;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.Gizmo.Components;

public class GizmoNone : MonoBehaviour
{
   public Transform TargetTransform;
   [Inject] private readonly PluginConfig _config = null!;
   [Inject] private readonly SignalBus _signalBus = null!;

   private void OnEnable()
   {
      transform.SetParent(TargetTransform, false);
      transform.position = TargetTransform.position;
      transform.rotation = TargetTransform.parent.rotation;
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
      transform.localScale = Vector3.one;
      transform.localScale = new Vector3(
         Mathf.Abs(_config.Gizmo.SizeBase * _config.Gizmo.GlobalScale / transform.lossyScale.x),
         Mathf.Abs(_config.Gizmo.SizeBase * _config.Gizmo.GlobalScale / transform.lossyScale.y),
         Mathf.Abs(_config.Gizmo.SizeBase * _config.Gizmo.GlobalScale / transform.lossyScale.z));
   }
}