using BeatmapEditor3D;
using EditorEnhanced.Commands;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.Gizmo.Components;

public class GizmoLaneScrollable : MonoBehaviour, IGizmoScrollInput, IGizmoPoolable
{
   [Inject] private readonly SignalBus _signalBus = null!;

   public EventBoxEditorData EventBoxEditorDataContext;

   public void ResetForPool()
   {
      EventBoxEditorDataContext = null;
   }

   public void OnScroll(float delta)
   {
      if (EventBoxEditorDataContext == null) return;
      _signalBus.Fire(new ScrollGizmoEventBoxLaneSignal(EventBoxEditorDataContext, delta));
   }
}
