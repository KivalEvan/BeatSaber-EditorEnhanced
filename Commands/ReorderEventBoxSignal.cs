using BeatmapEditor3D;

namespace EditorEnhanced.Commands;

public enum ReorderType
{
    Top,
    Up,
    Down,
    Bottom
}

public class ReorderEventBoxSignal(EventBoxEditorData eventBoxEditorData, ReorderType reorderType)
{
    public readonly EventBoxEditorData EventBoxEditorData = eventBoxEditorData;
    public readonly ReorderType ReorderType = reorderType;
}