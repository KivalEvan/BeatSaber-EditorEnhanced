using BeatmapEditor3D;

namespace EditorEnhanced.Commands;

public class PasteEventBoxSeedSignal
{
    public readonly EventBoxEditorData EventBoxEditorData;
    public readonly int Seed;

    public PasteEventBoxSeedSignal(EventBoxEditorData eventBoxEditorData, int seed)
    {
        Seed = seed;
        EventBoxEditorData = eventBoxEditorData;
    }
}