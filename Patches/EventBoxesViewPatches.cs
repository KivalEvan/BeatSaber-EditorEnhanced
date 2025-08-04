using BeatmapEditor3D.Views;
using EditorEnhanced.Commands;
using SiraUtil.Affinity;
using Zenject;

namespace EditorEnhanced.Patches;

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
        _signalBus.Fire<EventBoxSelectedSignal>();
    }
}