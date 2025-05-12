using System;
using BeatmapEditor3D;
using EditorEnhanced.Commands;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using Zenject;

namespace EditorEnhanced.UI.Views;

internal class LolighterViewController : IInitializable, IDisposable
{
    private readonly EditBeatmapNavigationViewController _ebnvc;
    private readonly EditorButtonBuilder _editorBtn;
    private readonly SignalBus _signalBus;

    public LolighterViewController(SignalBus signalBus,
        EditBeatmapNavigationViewController ebnvc,
        EditorButtonBuilder editorBtn)
    {
        _signalBus = signalBus;
        _ebnvc = ebnvc;
        _editorBtn = editorBtn;
    }

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