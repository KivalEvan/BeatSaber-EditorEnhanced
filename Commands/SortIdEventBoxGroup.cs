using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.LevelEditor;
using Zenject;

namespace EditorEnhanced.Commands;

public class SortIdEventBoxGroupSignal
{
}

public class SortIdEventBoxGroupCommand : IBeatmapEditorCommandWithHistory
{
   private readonly BeatmapEventBoxGroupsDataModel _beatmapEventBoxGroupsDataModel;
   private readonly EventBoxGroupsState _eventBoxGroupsState;
   private readonly SignalBus _signalBus;
   private BeatmapEditorObjectId _eventBoxGroupId;
   private List<(EventBoxEditorData eventBox, List<BaseEditorData> baseList)> _newEventBoxes;
   private List<(EventBoxEditorData eventBox, List<BaseEditorData> baseList)> _previousEventBoxes;

   public SortIdEventBoxGroupCommand(
      SignalBus signalBus,
      EventBoxGroupsState eventBoxGroupsState,
      BeatmapEventBoxGroupsDataModel beatmapEventBoxGroupsDataModel)
   {
      _signalBus = signalBus;
      _eventBoxGroupsState = eventBoxGroupsState;
      _beatmapEventBoxGroupsDataModel = beatmapEventBoxGroupsDataModel;
   }

   public bool shouldAddToHistory { get; private set; }

   public void Execute()
   {
      var eventBoxGroupId = _eventBoxGroupsState.eventBoxGroupContext.id;
      var byEventBoxGroupId = _beatmapEventBoxGroupsDataModel.GetEventBoxesByEventBoxGroupId(eventBoxGroupId);
      if (byEventBoxGroupId.Count == 0) return;
      var previousEventBoxes = new List<(EventBoxEditorData, List<BaseEditorData>)>(byEventBoxGroupId.Count);
      var newEventBoxes = new List<(EventBoxEditorData, List<BaseEditorData>)>();

      foreach (var eventBoxEditorData in byEventBoxGroupId)
      {
         var list = _beatmapEventBoxGroupsDataModel.GetBaseEventsListByEventBoxId(eventBoxEditorData.id).ToList();
         previousEventBoxes.Add((eventBoxEditorData, list));
         newEventBoxes.Add((eventBoxEditorData, list));
      }

      newEventBoxes = newEventBoxes
         .OrderByDescending(eventBox =>
            eventBox.Item1.indexFilter.type == IndexFilterEditorData.IndexFilterType.Division
               ? eventBox.Item1.indexFilter.param0
               : eventBox.Item1.indexFilter.param1)
         .ThenBy(eventBox =>
            eventBox.Item1.indexFilter.type == IndexFilterEditorData.IndexFilterType.Division
               ? eventBox.Item1.indexFilter.param1
               : eventBox.Item1.indexFilter.param0)
         .ToList();

      _eventBoxGroupId = eventBoxGroupId;
      _previousEventBoxes = previousEventBoxes;
      _newEventBoxes = newEventBoxes;
      shouldAddToHistory = true;
      Redo();
   }

   public void Undo()
   {
      foreach (var newEventBox in _newEventBoxes)
      {
         _beatmapEventBoxGroupsDataModel.RemoveBaseEditorDataList(newEventBox.eventBox.id, newEventBox.baseList);
         _beatmapEventBoxGroupsDataModel.RemoveEventBox(_eventBoxGroupId, newEventBox.eventBox);
      }

      foreach (var previousEventBox in _previousEventBoxes)
      {
         _beatmapEventBoxGroupsDataModel.InsertEventBox(_eventBoxGroupId, previousEventBox.eventBox);
         if (previousEventBox.baseList != null)
            _beatmapEventBoxGroupsDataModel.InsertBaseEditorDataList(
               previousEventBox.eventBox.id,
               previousEventBox.baseList);
      }

      _signalBus.Fire(new EventBoxesUpdatedSignal(0));
      _signalBus.Fire<BeatmapLevelUpdatedSignal>();
   }

   public void Redo()
   {
      foreach (var previousEventBox in _previousEventBoxes)
      {
         _beatmapEventBoxGroupsDataModel.RemoveBaseEditorDataList(
            previousEventBox.eventBox.id,
            previousEventBox.baseList);
         _beatmapEventBoxGroupsDataModel.RemoveEventBox(_eventBoxGroupId, previousEventBox.eventBox);
      }

      foreach (var newEventBox in _newEventBoxes)
      {
         _beatmapEventBoxGroupsDataModel.InsertEventBox(_eventBoxGroupId, newEventBox.eventBox);
         if (newEventBox.baseList != null)
            _beatmapEventBoxGroupsDataModel.InsertBaseEditorDataList(newEventBox.eventBox.id, newEventBox.baseList);
      }

      _signalBus.Fire(new EventBoxesUpdatedSignal(0));
      _signalBus.Fire<BeatmapLevelUpdatedSignal>();
   }
}