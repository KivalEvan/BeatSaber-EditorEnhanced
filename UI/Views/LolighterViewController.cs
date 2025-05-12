using System;
using BeatmapEditor3D;
using EditorEnhanced.Commands;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using Zenject;

namespace EditorEnhanced.UI.Views;

internal class LolighterViewController : IInitializable, IDisposable
{
    [Inject] private readonly EditBeatmapNavigationViewController _ebnvc;
    [Inject] private readonly EditorButtonBuilder _editorBtn;
    [Inject] private readonly SignalBus _signalBus;

    public void Dispose()
    {
    }

    public void Initialize()
    {
        var target = _ebnvc._eventsToolbarView;

        _editorBtn.CreateNew()
            .SetFontSize(10)
            .SetText("Commit Crime")
            .SetOnClick(() => _signalBus.Fire(new LolighterSignal()))
            .CreateObject(target.transform);
    }
}