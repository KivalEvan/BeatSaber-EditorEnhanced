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

public class DuplicateEventBoxSignal
{
   public readonly BeatmapEditorObjectId EventBoxId;
   public readonly bool CopyEvent;
   public readonly bool Increment;
   public readonly bool RandomSeed;

   public DuplicateEventBoxSignal(
      BeatmapEditorObjectId eventBoxId,
      bool copyEvent,
      bool randomSeed,
      bool increment)
   {
      EventBoxId = eventBoxId;
      CopyEvent = copyEvent;
      RandomSeed = randomSeed;
      Increment = increment;
   }
}

public class DuplicateEventBoxCommand : IBeatmapEditorCommandWithHistory
{
   private readonly BeatmapEventBoxGroupsDataModel _beatmapEventBoxGroupsDataModel;
   private readonly EventBoxGroupsState _eventBoxGroupsState;
   private readonly DuplicateEventBoxSignal _signal;
   private readonly SignalBus _signalBus;
   private BeatmapEditorObjectId _eventBoxGroupId;
   private (EventBoxEditorData eventBox, List<BaseEditorData> events) _newEventBox;
   private int _newIdx;

   public DuplicateEventBoxCommand(
      DuplicateEventBoxSignal signal,
      SignalBus signalBus,
      EventBoxGroupsState eventBoxGroupsState,
      BeatmapEventBoxGroupsDataModel beatmapEventBoxGroupsDataModel)
   {
      _signal = signal;
      _signalBus = signalBus;
      _eventBoxGroupsState = eventBoxGroupsState;
      _beatmapEventBoxGroupsDataModel = beatmapEventBoxGroupsDataModel;
   }

   public bool shouldAddToHistory { get; private set; }

   public void Execute()
   {
      _eventBoxGroupId = _eventBoxGroupsState.eventBoxGroupContext.id;
      _newIdx = _beatmapEventBoxGroupsDataModel.GetEventBoxIdxByEventBoxId(_signal.EventBoxId) + 1;
      var newEventBox = EventBoxGroupsClipboardHelper.CopyEventBoxEditorDataWithoutId(
         _beatmapEventBoxGroupsDataModel
            .GetEventBoxesCollectionByEventBoxGroupId(_eventBoxGroupId)
            .eventBoxes[_newIdx - 1]);

      if (_signal.Increment)
      {
         if (newEventBox.indexFilter.type == IndexFilterEditorData.IndexFilterType.Division)
            newEventBox.indexFilter.SetField("param1", newEventBox.indexFilter.param1 + 1);
         else
            newEventBox.indexFilter.SetField("param0", newEventBox.indexFilter.param0 + 1);
      }

      if (_signal.RandomSeed
          && newEventBox.indexFilter.randomType.HasFlag(IndexFilter.IndexFilterRandomType.RandomElements))
         newEventBox.indexFilter.SetField("seed", Random.Range(int.MinValue, int.MaxValue));

      _newEventBox = (newEventBox,
         _signal.CopyEvent
            ? _beatmapEventBoxGroupsDataModel
               .GetBaseEventsListByEventBoxId(_signal.EventBoxId)
               .Select(d => EventBoxGroupsClipboardHelper.CopyBaseEditorDataWithoutId(d))
               .ToList()
            : []);
      shouldAddToHistory = true;
      Redo();
   }

   public void Undo()
   {
      _beatmapEventBoxGroupsDataModel.RemoveBaseEditorDataList(_newEventBox.eventBox.id, _newEventBox.events);
      _beatmapEventBoxGroupsDataModel.RemoveEventBox(_eventBoxGroupId, _newEventBox.eventBox);
      _signalBus.Fire(new EventBoxesUpdatedSignal(_newIdx - 1));
      _signalBus.Fire<BeatmapLevelUpdatedSignal>();
   }

   public void Redo()
   {
      _beatmapEventBoxGroupsDataModel.InsertEventBox(_eventBoxGroupId, _newEventBox.eventBox, _newIdx);
      _beatmapEventBoxGroupsDataModel.InsertBaseEditorDataList(_newEventBox.eventBox.id, _newEventBox.events);
      _signalBus.Fire(new EventBoxesUpdatedSignal(_newIdx));
      _signalBus.Fire<BeatmapLevelUpdatedSignal>();
   }
}