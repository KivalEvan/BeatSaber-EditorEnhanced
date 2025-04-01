using System;
using BeatmapEditor3D;
using EditorEnhanced.Commands;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using Tweening;
using Zenject;

namespace EditorEnhanced.UI.Views;

internal class LolighterViewController(SignalBus signalBus, BeatmapFlowCoordinator bfc, TimeTweeningManager twm) : IInitializable, IDisposable
{
    public void Initialize()
    {
        var target = bfc._editBeatmapNavigationViewController._eventsToolbarView;

        new EditorButtonTag(bfc, twm)
            .SetText("Commit Crime")
            .SetOnClick(() => signalBus.Fire(new LolighterSignal()))
            .CreateObject(target.transform);
    }

    public void Dispose()
    {
    }
}