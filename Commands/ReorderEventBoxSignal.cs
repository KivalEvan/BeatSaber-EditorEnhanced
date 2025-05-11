using BeatmapEditor3D;

namespace EditorEnhanced.Commands;

public enum ReorderType
{
    Top,
    Up,
    Down,
    Bottom,
    Any,
}

public class ReorderEventBoxSignal(EventBoxEditorData eventBoxEditorData, ReorderType reorderType, int index = 0)
{
    public readonly EventBoxEditorData EventBoxEditorData = eventBoxEditorData;
    public readonly ReorderType ReorderType = reorderType;
    public readonly int Index = index;
}