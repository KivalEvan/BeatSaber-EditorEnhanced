using BeatmapEditor3D;

namespace EditorEnhanced.Commands;

public class CopyEventBoxSignal
{
    public readonly EventBoxEditorData EventBoxEditorData;

    public CopyEventBoxSignal(EventBoxEditorData eventBoxEditorData)
    {
        EventBoxEditorData = eventBoxEditorData;
    }
}