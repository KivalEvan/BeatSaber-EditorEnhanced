using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.LevelEditor;
using IPA.Utilities;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.Commands;

public class ScrollGizmoEventBoxLaneSignal
{
   public readonly float Direction;
   public readonly EventBoxEditorData EventBoxEditorData;

   public ScrollGizmoEventBoxLaneSignal(EventBoxEditorData eventBoxEditorData, float direction)
   {
      EventBoxEditorData = eventBoxEditorData;
      Direction = direction;
   }
}

public class ScrollGizmoEventBoxLaneCommand : IBeatmapEditorCommandWithHistory
{
   private readonly BeatmapEventBoxGroupsDataModel _dataModel;
   private readonly BeatmapState _beatmapState;
   private readonly ScrollGizmoEventBoxLaneSignal _signal;
   private readonly SignalBus _signalBus;
   private BeatmapEditorObjectId _eventBoxId;
   private List<BaseEditorData> _modifiedEvents;
   private List<BaseEditorData> _originalEvents;

   public ScrollGizmoEventBoxLaneCommand(
      ScrollGizmoEventBoxLaneSignal signal,
      BeatmapEventBoxGroupsDataModel dataModel,
      BeatmapState beatmapState,
      SignalBus signalBus)
   {
      _signal = signal;
      _dataModel = dataModel;
      _beatmapState = beatmapState;
      _signalBus = signalBus;
   }

   public bool shouldAddToHistory { get; private set; }

   public void Execute()
   {
      if (_signal.EventBoxEditorData == null || Mathf.Approximately(_signal.Direction, 0f)) return;

      _eventBoxId = _signal.EventBoxEditorData.id;
      _originalEvents = _dataModel.GetBaseEventsListByEventBoxId(_eventBoxId).ToList();
      if (_originalEvents.Count == 0) return;

      _modifiedEvents = _originalEvents.Select(AdjustEvent).ToList();
      shouldAddToHistory = true;
      Redo();
   }

   public void Undo()
   {
      Replace(_modifiedEvents, _originalEvents);
   }

   public void Redo()
   {
      Replace(_originalEvents, _modifiedEvents);
   }

   private BaseEditorData AdjustEvent(BaseEditorData source)
   {
      var adjusted = EventBoxGroupsClipboardHelper.CopyBaseEditorDataWithoutId(source);
      var direction = Mathf.Sign(_signal.Direction);

      switch (adjusted)
      {
         case LightColorBaseEditorData color:
            color.SetField(
               "brightness",
               LightColorEventHelper.IncreaseBrightnessByPrecision(
                  color.brightness,
                  direction,
                  _beatmapState.scrollPrecision));
            break;
         case LightRotationBaseEditorData rotation:
            var rotationDelta = ModifyHoveredLightRotationDeltaRotationCommand._precisions[
               _beatmapState.scrollPrecision] * direction;
            rotation.SetField("rotation", Mathf.Repeat(rotation.rotation + rotationDelta, 360f));
            break;
         case LightTranslationBaseEditorData translation:
            var translationDelta = ModifyHoveredLightTranslationDeltaTranslationCommand._precisions[
               _beatmapState.scrollPrecision] * direction;
            translation.SetField(
               "translation",
               Mathf.Round(translation.translation * 1_000f + translationDelta * 10f) / 1_000f);
            break;
         case FloatFxBaseEditorData fx:
            var fxDelta = ModifyHoveredFloatFxDeltaValueCommand._precisions[
               _beatmapState.scrollPrecision] * direction;
            fx.SetField("value", Mathf.Round(fx.value * 100f + fxDelta) / 100f);
            break;
      }

      return adjusted;
   }

   private void Replace(List<BaseEditorData> current, List<BaseEditorData> replacement)
   {
      _dataModel.RemoveBaseEditorDataList(_eventBoxId, current);
      _dataModel.InsertBaseEditorDataList(_eventBoxId, replacement);
      _signalBus.Fire<BeatmapLevelUpdatedSignal>();
   }
}
