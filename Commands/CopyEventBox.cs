using BeatmapEditor3D;
using EditorEnhanced.Managers;

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

    public CopyEventBoxCommand(CopyEventBoxSignal signal, EventBoxClipboardManager clipboardManager)
    {
        _signal = signal;
        _clipboardManager = clipboardManager;
    }

    public void Execute()
    {
        _clipboardManager.Copy(_signal.EventBoxEditorData);
    }
}