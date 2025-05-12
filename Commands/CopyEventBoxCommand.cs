using BeatmapEditor3D;
using EditorEnhanced.Managers;
using Zenject;

namespace EditorEnhanced.Commands;

public class CopyEventBoxCommand(
    SignalBus signalBus,
    CopyEventBoxSignal signal,
    EventBoxClipboardManager clipboardManager) : IBeatmapEditorCommand
{
    public void Execute()
    {
        clipboardManager.Add(signal.EventBoxEditorData);
    }
}