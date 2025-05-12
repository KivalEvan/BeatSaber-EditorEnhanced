using BeatmapEditor3D;
using EditorEnhanced.Managers;
using Zenject;

namespace EditorEnhanced.Commands;

public class CopyEventBoxCommand : IBeatmapEditorCommand
{
    [Inject] private readonly EventBoxClipboardManager _clipboardManager;
    [Inject] private readonly CopyEventBoxSignal _signal;

    public void Execute()
    {
        _clipboardManager.Add(_signal.EventBoxEditorData);
    }
}