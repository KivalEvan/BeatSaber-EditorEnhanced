using BeatmapEditor3D.Views;
using EditorEnhanced.Gizmo.Commands;
using SiraUtil.Affinity;
using Zenject;

namespace EditorEnhanced.Gizmo.Patches;

public class EventBoxesViewPatches : IAffinity
{
    private readonly SignalBus _signalBus;

    public EventBoxesViewPatches(SignalBus signalBus)
    {
        _signalBus = signalBus;
    }

    [AffinityPostfix]
    [AffinityPatch(typeof(EventBoxesView), nameof(EventBoxesView.SetEventBoxData))]
    private void SignalSelectedEventBox()
    {
        _signalBus.Fire<GizmoEventBoxSelectedSignal>();
    }
}