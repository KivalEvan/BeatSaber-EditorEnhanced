using BeatmapEditor3D;

namespace EditorEnhanced.Commands;

public class PasteEventBoxSeedSignal(EventBoxEditorData eventBoxEditorData, int seed)
{
    public readonly int Seed = seed;
    public readonly EventBoxEditorData EventBoxEditorData = eventBoxEditorData;
}