using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.LevelEditor;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.Commands;

public class DragGizmoLightRotationEventBoxSignal
{
   public readonly EventBoxEditorData EventBoxEditorData;
   public readonly float Value;

   public DragGizmoLightRotationEventBoxSignal(EventBoxEditorData eventBoxEditorData, float value)
   {
      EventBoxEditorData = eventBoxEditorData;
      Value = value;
   }
}

public class DragGizmoLightRotationEventBoxCommand : IBeatmapEditorCommand, IBeatmapEditorCommandWithHistory
{
   private readonly BeatmapEventBoxGroupsDataModel _beatmapEventBoxGroupsDataModel;
   private readonly BeatmapState _beatmapState;
   private readonly EventBoxGroupsState _eventBoxGroupsState;
   private readonly DragGizmoLightRotationEventBoxSignal _signal;
   private readonly SignalBus _signalBus;
   private BaseEditorData _conflict;
   private BaseEditorData _event;
   private BeatmapEditorObjectId _eventBoxId;

   public DragGizmoLightRotationEventBoxCommand(
      DragGizmoLightRotationEventBoxSignal signal,
      SignalBus signalBus,
      BeatmapState beatmapState,
      EventBoxGroupsState eventBoxGroupsState,
      BeatmapEventBoxGroupsDataModel beatmapEventBoxGroupsDataModel)
   {
      _signal = signal;
      _signalBus = signalBus;
      _beatmapState = beatmapState;
      _eventBoxGroupsState = eventBoxGroupsState;
      _beatmapEventBoxGroupsDataModel = beatmapEventBoxGroupsDataModel;
   }

   public void Execute()
   {
      _eventBoxId = _signal.EventBoxEditorData.id;
      var beat = _beatmapState.beat - _eventBoxGroupsState.eventBoxGroupContext.beat;
      _conflict = _beatmapEventBoxGroupsDataModel.GetBaseEditorDataAt(_eventBoxId, beat);
      _event = LightRotationBaseEditorData.CreateNew(
         beat,
         _eventBoxGroupsState.lightRotationEaseLeadType,
         _eventBoxGroupsState.lightRotationEaseCurveType,
         _eventBoxGroupsState.lightRotationLoopCount,
         Mathf.Round(_signal.Value * 1_000f) / 1_000f,
         false,
         _eventBoxGroupsState.lightRotationDirection);
      shouldAddToHistory = true;
      Redo();
   }

   public bool shouldAddToHistory { get; private set; }

   public void Redo()
   {
      if (_conflict != null) _beatmapEventBoxGroupsDataModel.RemoveBaseEditorData(_eventBoxId, _conflict);
      _beatmapEventBoxGroupsDataModel.InsertBaseEditorData(_eventBoxId, _event);
      _signalBus.Fire<BeatmapLevelUpdatedSignal>();
   }

   public void Undo()
   {
      _beatmapEventBoxGroupsDataModel.RemoveBaseEditorData(_eventBoxId, _event);
      if (_conflict != null) _beatmapEventBoxGroupsDataModel.InsertBaseEditorData(_eventBoxId, _conflict);
      _signalBus.Fire<BeatmapLevelUpdatedSignal>();
   }
}