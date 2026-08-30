using System.Linq;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using Zenject;

namespace EditorEnhanced.MotionPath;

internal sealed class MotionPathEventCommitter
{
   private readonly BeatmapEventBoxGroupsDataModel _dataModel;
   private readonly SignalBus _signalBus;

   public MotionPathEventCommitter(
      BeatmapEventBoxGroupsDataModel dataModel,
      SignalBus signalBus)
   {
      _dataModel = dataModel;
      _signalBus = signalBus;
   }

   public bool TryCommit(MotionPathEventSource source, float value)
   {
      var group = _dataModel.GetEventBoxGroupById(source.EventBoxGroupId);
      if (group == null || group.type != source.GroupType) return false;
      var eventBox = _dataModel
         .GetEventBoxesByEventBoxGroupId(source.EventBoxGroupId)
         .FirstOrDefault(item => item.id == source.EventBoxId);
      if (eventBox == null
          || !_dataModel.GetBaseEventsListByEventBoxId(source.EventBoxId)
             .Any(item => item.id == source.BaseEventId))
         return false;

      switch (_dataModel.GetBaseEditorDataById(source.BaseEventId))
      {
         case LightRotationBaseEditorData rotation
             when eventBox is LightRotationEventBoxEditorData rotationBox
                  && rotationBox.axis == source.Axis
                  && !rotation.usePreviousValue
                  && source.ValueType == MotionPathEventValueType.Rotation:
            _signalBus.Fire(
               new ModifyFullLightRotationSignal(
                  source.EventBoxGroupId,
                  source.EventBoxId,
                  rotation.id,
                  rotation.beat,
                  rotation.easeLeadType,
                  rotation.easeCurveType,
                  value,
                  rotation.loopsCount,
                  rotation.rotationDirection,
                  rotation.usePreviousValue));
            return true;
         case LightTranslationBaseEditorData translation
             when eventBox is LightTranslationEventBoxEditorData translationBox
                  && translationBox.axis == source.Axis
                  && !translation.usePreviousValue
                  && source.ValueType == MotionPathEventValueType.Translation:
            _signalBus.Fire(
               new ModifyFullLightTranslationSignal(
                  source.EventBoxGroupId,
                  source.EventBoxId,
                  translation.id,
                  translation.beat,
                  translation.easeLeadType,
                  translation.easeCurveType,
                  value,
                  translation.usePreviousValue));
            return true;
         default:
            return false;
      }
   }
}
