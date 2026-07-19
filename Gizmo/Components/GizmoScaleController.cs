using EditorEnhanced.Configuration;
using EditorEnhanced.Gizmo.Commands;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.Gizmo.Components;

public class GizmoScaleController : MonoBehaviour
{
   [Inject] private readonly PluginConfig _config = null!;
   [Inject] private readonly SignalBus _signalBus = null!;
   private GizmoDraggable _draggable;

   public void SetDraggable(GizmoDraggable draggable)
   {
      _draggable = draggable;
   }

   private void OnEnable()
   {
      UpdateSize();

      _signalBus.Subscribe<GizmoScaleConfigChangedSignal>(UpdateSize);
   }

   private void OnDisable()
   {
      _signalBus.TryUnsubscribe<GizmoScaleConfigChangedSignal>(UpdateSize);
   }

   private void UpdateSize()
   {
      AdjustSize();
      if (_draggable != null) _draggable.RefreshSize();
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
