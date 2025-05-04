using System;
using BeatmapEditor3D;
using EditorEnhanced.Commands;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using Tweening;
using Zenject;

namespace EditorEnhanced.UI.Views;

internal class LolighterViewController(SignalBus signalBus, BeatmapFlowCoordinator bfc, EditorButtonBuilder editorBtn)
    : IInitializable, IDisposable
{
    public void Initialize()
    {
        var target = bfc._editBeatmapNavigationViewController._eventsToolbarView;

        editorBtn.CreateNew()
            .SetFontSize(10)
            .SetText("Commit Crime")
            .SetOnClick(() => signalBus.Fire(new LolighterSignal()))
            .CreateObject(target.transform);
    }

    public void Dispose()
    {
    }
}