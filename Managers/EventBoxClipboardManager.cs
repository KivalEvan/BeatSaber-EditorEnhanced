using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using BeatSaber.TrackDefinitions.DataModels;
using EditorEnhanced.EventBoxes;

namespace EditorEnhanced.Managers;

public sealed class EventBoxClipboardManager
{
   private readonly EventBoxGroupMutation _mutation;
   private EventBoxSnapshot _fxEventBoxClipboard;
   private EventBoxSnapshot _lightColorEventBoxClipboard;
   private EventBoxSnapshot _lightRotationEventBoxClipboard;
   private EventBoxSnapshot _lightTranslationEventBoxClipboard;

   public EventBoxClipboardManager(EventBoxGroupMutation mutation)
   {
      _mutation = mutation;
   }

   public void Copy(EventBoxEditorData eventBoxEditorData)
   {
      var snapshot = _mutation.Capture(eventBoxEditorData);
      switch (eventBoxEditorData)
      {
         case LightColorEventBoxEditorData:
            _lightColorEventBoxClipboard = snapshot;
            break;
         case LightRotationEventBoxEditorData:
            _lightRotationEventBoxClipboard = snapshot;
            break;
         case LightTranslationEventBoxEditorData:
            _lightTranslationEventBoxClipboard = snapshot;
            break;
         case FxEventBoxEditorData:
            _fxEventBoxClipboard = snapshot;
            break;
      }
   }

   public EventBoxSnapshot Get(EventBoxGroupType type)
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
