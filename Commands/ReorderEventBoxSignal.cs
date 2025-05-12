using BeatmapEditor3D;

namespace EditorEnhanced.Commands;

public enum ReorderType
{
    Top,
    Up,
    Down,
    Bottom,
    Any
}

public class ReorderEventBoxSignal
{
    public readonly EventBoxEditorData EventBoxEditorData;
    public readonly int Index;
    public readonly ReorderType ReorderType;

    public ReorderEventBoxSignal(EventBoxEditorData eventBoxEditorData, ReorderType reorderType, int index = 0)
    {
        EventBoxEditorData = eventBoxEditorData;
        ReorderType = reorderType;
        Index = index;
    }
}