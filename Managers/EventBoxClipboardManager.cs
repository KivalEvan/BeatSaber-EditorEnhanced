using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using BeatSaber.TrackDefinitions.DataModels;
using Zenject;

namespace EditorEnhanced.Managers;

public class EventBoxClipboardManager(BeatmapEventBoxGroupsDataModel beatmapEventBoxGroupsDataModel) : IInitializable
{
    private (EventBoxEditorData, List<BaseEditorData>)? fxEventBoxClipboard;
    private (EventBoxEditorData, List<BaseEditorData>)? lightColorEventBoxClipboard;
    private (EventBoxEditorData, List<BaseEditorData>)? lightRotationEventBoxClipboard;

    private (EventBoxEditorData, List<BaseEditorData>)?
        lightTranslationEventBoxClipboard;

    public void Initialize()
    {
    }

    public void Add(EventBoxEditorData eventBoxEditorData)
    {
        var l = beatmapEventBoxGroupsDataModel.GetBaseEventsListByEventBoxId(eventBoxEditorData.id).ToList();
        switch (eventBoxEditorData)
        {
            case LightColorEventBoxEditorData data:
                lightColorEventBoxClipboard = (data, l);
                break;
            case LightRotationEventBoxEditorData data:
                lightRotationEventBoxClipboard = (data, l);
                break;
            case LightTranslationEventBoxEditorData data:
                lightTranslationEventBoxClipboard = (data, l);
                break;
            case FxEventBoxEditorData data:
                fxEventBoxClipboard = (data, l);
                break;
        }
    }

    public (EventBoxEditorData box, List<BaseEditorData> events)? Paste(EventBoxGroupType type)
    {
        return type switch
        {
            EventBoxGroupType.Color => lightColorEventBoxClipboard,
            EventBoxGroupType.Rotation => lightRotationEventBoxClipboard,
            EventBoxGroupType.Translation => lightTranslationEventBoxClipboard,
            EventBoxGroupType.FloatFx => fxEventBoxClipboard
        };
    }
}