using System;
using BeatmapEditor3D;
using EditorEnhanced.Commands;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using Zenject;

namespace EditorEnhanced.UI.Views;

internal class LolighterViewController(
    SignalBus signalBus,
    EditBeatmapNavigationViewController ebnvc,
    EditorButtonBuilder editorBtn)
    : IInitializable, IDisposable
{
    public void Dispose()
    {
    }

    public void Initialize()
    {
        var target = ebnvc._eventsToolbarView;

        editorBtn.CreateNew()
            .SetFontSize(10)
            .SetText("Commit Crime")
            .SetOnClick(() => signalBus.Fire(new LolighterSignal()))
            .CreateObject(target.transform);
    }
}