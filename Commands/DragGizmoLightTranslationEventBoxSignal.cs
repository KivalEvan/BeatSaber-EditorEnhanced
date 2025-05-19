using BeatmapEditor3D;
using UnityEngine;

namespace EditorEnhanced.Commands;

public class DragGizmoLightTranslationEventBoxSignal
{
    public readonly EventBoxEditorData EventBoxEditorData;
    public readonly float Value;
    
    public DragGizmoLightTranslationEventBoxSignal(EventBoxEditorData eventBoxEditorData, float value)
    {
        EventBoxEditorData = eventBoxEditorData;
        Value = value;
    }
}