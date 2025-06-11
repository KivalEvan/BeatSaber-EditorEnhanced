using BeatmapEditor3D;

namespace EditorEnhanced.Commands;

public class DuplicateEventBoxSignal
{
    public readonly EventBoxEditorData EventBoxEditorData;
    public readonly bool CopyEvent;
    public readonly bool RandomSeed;
    public readonly bool Increment;

    public DuplicateEventBoxSignal(EventBoxEditorData eventBoxEditorData, bool copyEvent, bool randomSeed, bool increment)
    {
        EventBoxEditorData = eventBoxEditorData;
        CopyEvent = copyEvent;
        RandomSeed = randomSeed;
        Increment = increment;
    }
}