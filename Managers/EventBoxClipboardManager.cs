using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using BeatSaber.TrackDefinitions.DataModels;

namespace EditorEnhanced.Managers;

public class EventBoxClipboardManager
{
   private readonly BeatmapEventBoxGroupsDataModel _beatmapEventBoxGroupsDataModel;
   private (EventBoxEditorData, List<BaseEditorData>)? _fxEventBoxClipboard;
   private (EventBoxEditorData, List<BaseEditorData>)? _lightColorEventBoxClipboard;
   private (EventBoxEditorData, List<BaseEditorData>)? _lightRotationEventBoxClipboard;
   private (EventBoxEditorData, List<BaseEditorData>)? _lightTranslationEventBoxClipboard;

   public EventBoxClipboardManager(BeatmapEventBoxGroupsDataModel beatmapEventBoxGroupsDataModel)
   {
      _beatmapEventBoxGroupsDataModel = beatmapEventBoxGroupsDataModel;
   }

   public void Copy(EventBoxEditorData eventBoxEditorData)
   {
      var l = _beatmapEventBoxGroupsDataModel.GetBaseEventsListByEventBoxId(eventBoxEditorData.id).ToList();
      switch (eventBoxEditorData)
      {
         case LightColorEventBoxEditorData data:
            _lightColorEventBoxClipboard = (data, l);
            break;
         case LightRotationEventBoxEditorData data:
            _lightRotationEventBoxClipboard = (data, l);
            break;
         case LightTranslationEventBoxEditorData data:
            _lightTranslationEventBoxClipboard = (data, l);
            break;
         case FxEventBoxEditorData data:
            _fxEventBoxClipboard = (data, l);
            break;
      }
   }

   public (EventBoxEditorData box, List<BaseEditorData> events)? Paste(EventBoxGroupType type)
   {
      return type switch
      {
         EventBoxGroupType.Color => _lightColorEventBoxClipboard,
         EventBoxGroupType.Rotation => _lightRotationEventBoxClipboard,
         EventBoxGroupType.Translation => _lightTranslationEventBoxClipboard,
         EventBoxGroupType.FloatFx => _fxEventBoxClipboard,
         _ => null
      };
   }
}
