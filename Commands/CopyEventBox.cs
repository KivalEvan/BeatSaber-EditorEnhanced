using BeatmapEditor3D;
using EditorEnhanced.Managers;
using Zenject;

namespace EditorEnhanced.Commands;

public class CopyEventBoxSignal
{
    public readonly EventBoxEditorData EventBoxEditorData;

    public CopyEventBoxSignal(EventBoxEditorData eventBoxEditorData)
    {
        EventBoxEditorData = eventBoxEditorData;
    }
}

public class CopyEventBoxCommand : IBeatmapEditorCommand
{
    private readonly EventBoxClipboardManager _clipboardManager;
    private readonly CopyEventBoxSignal _signal;

    public CopyEventBoxCommand(SignalBus signalBus,
        CopyEventBoxSignal signal,
        EventBoxClipboardManager clipboardManager)
    {
        _signal = signal;
        _clipboardManager = clipboardManager;
    }

    public void Execute()
    {
        _clipboardManager.Add(_signal.EventBoxEditorData);
    }
}