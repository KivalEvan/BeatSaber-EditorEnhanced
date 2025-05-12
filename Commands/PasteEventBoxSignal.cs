using BeatmapEditor3D;

namespace EditorEnhanced.Commands;

public class PasteEventBoxSignal
{
    public readonly EventBoxEditorData EventBoxEditorData;

    public PasteEventBoxSignal(EventBoxEditorData eventBoxEditorData)
    {
        EventBoxEditorData = eventBoxEditorData;
    }
}