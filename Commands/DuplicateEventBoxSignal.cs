using BeatmapEditor3D;

namespace EditorEnhanced.Commands;

public class DuplicateEventBoxSignal
{
    public readonly EventBoxEditorData EventBoxEditorData;

    public DuplicateEventBoxSignal(EventBoxEditorData eventBoxEditorData)
    {
        EventBoxEditorData = eventBoxEditorData;
    }
}