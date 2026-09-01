using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.LevelEditor;
using Zenject;

namespace EditorEnhanced.Commands;

public sealed class MergeSelectedEventBoxGroupsSignal
{
   public MergeSelectedEventBoxGroupsSignal(BeatmapEditorObjectId targetGroupId)
   {
      TargetGroupId = targetGroupId;
   }

   public BeatmapEditorObjectId TargetGroupId { get; }
}

public sealed class MergeSelectedEventBoxGroupsCommand : IBeatmapEditorCommandWithHistory
{
   private readonly BeatmapEventBoxGroupsDataModel _dataModel;
   private readonly EventBoxGroupsClipboardHelper _eventBoxGroupsClipboardHelper;
   private readonly EventBoxGroupsSelectionState _selectionState;
   private readonly MergeSelectedEventBoxGroupsSignal _signal;
   private readonly SignalBus _signalBus;
   private Dictionary<BeatmapEditorObjectId, List<BaseEditorData>> _mergedBaseEvents;
   private Dictionary<BeatmapEditorObjectId, List<EventBoxEditorData>> _mergedEventBoxes;
   private List<EventBoxGroupEditorData> _mergedGroups;
   private Dictionary<BeatmapEditorObjectId, List<EventBoxEditorData>> _originalEventBoxes;
   private Dictionary<BeatmapEditorObjectId, List<BaseEditorData>> _originalBaseEvents;
   private List<EventBoxGroupEditorData> _originalGroups;
   private List<BeatmapEditorObjectId> _originalSelection;

   public MergeSelectedEventBoxGroupsCommand(
      MergeSelectedEventBoxGroupsSignal signal,
      EventBoxGroupsSelectionState selectionState,
      BeatmapEventBoxGroupsDataModel dataModel,
      EventBoxGroupsClipboardHelper eventBoxGroupsClipboardHelper,
      SignalBus signalBus)
   {
      _signal = signal;
      _selectionState = selectionState;
      _dataModel = dataModel;
      _eventBoxGroupsClipboardHelper = eventBoxGroupsClipboardHelper;
      _signalBus = signalBus;
   }

   public bool shouldAddToHistory { get; private set; }

   public void Execute()
   {
      _originalSelection = _selectionState.eventBoxGroups.ToList();
      if (_originalSelection.Count < 2) return;

      var selectedGroups = _originalSelection
         .Select(_dataModel.GetEventBoxGroupById)
         .Where(group => group != null)
         .ToList();
      if (selectedGroups.Count != _originalSelection.Count) return;

      var target = selectedGroups.FirstOrDefault(group => group.id == _signal.TargetGroupId);
      if (target == null) return;
      if (selectedGroups.Any(group => group.groupId != target.groupId || group.type != target.type)) return;

      _originalGroups = [target, .. selectedGroups.Where(group => group.id != target.id)];

      _originalEventBoxes = _originalGroups.ToDictionary(
         group => group.id,
         group => _dataModel.GetEventBoxesByEventBoxGroupId(group.id).ToList());
      _originalBaseEvents = _originalEventBoxes.Values
         .SelectMany(eventBoxes => eventBoxes)
         .ToDictionary(
            eventBox => eventBox.id,
            eventBox => _dataModel.GetBaseEventsListByEventBoxId(eventBox.id).ToList());

      var prioritized = _originalGroups
         .Select((group, selectionIndex) => new { Group = group, SelectionIndex = selectionIndex })
         .OrderByDescending(item => item.Group.beat)
         .ThenBy(item => item.SelectionIndex)
         .SelectMany(item => _originalEventBoxes[item.Group.id])
         .ToList();
      var merged = new List<EventBoxEditorData>(prioritized.Count);
      foreach (var candidate in prioritized)
         if (!merged.Any(existing => EventBoxTargetsMatch(existing, candidate)))
            merged.Add(candidate);

      _mergedBaseEvents = merged.ToDictionary(
         eventBox => eventBox.id,
         eventBox => _originalBaseEvents[eventBox.id]);

      _mergedGroups = [target];
      _mergedEventBoxes = new Dictionary<BeatmapEditorObjectId, List<EventBoxEditorData>>
      {
         [target.id] = merged
      };
      shouldAddToHistory = true;
      Redo();
   }

   public void Undo()
   {
      Replace(
         _mergedGroups,
         _mergedEventBoxes,
         _mergedBaseEvents,
         _originalGroups,
         _originalEventBoxes,
         _originalBaseEvents,
         _originalSelection);
   }

   public void Redo()
   {
      Replace(
         _originalGroups,
         _originalEventBoxes,
         _originalBaseEvents,
         _mergedGroups,
         _mergedEventBoxes,
         _mergedBaseEvents,
         [_mergedGroups[0].id]);
   }

   private void Replace(
      List<EventBoxGroupEditorData> currentGroups,
      Dictionary<BeatmapEditorObjectId, List<EventBoxEditorData>> currentEventBoxes,
      Dictionary<BeatmapEditorObjectId, List<BaseEditorData>> currentBaseEvents,
      List<EventBoxGroupEditorData> replacementGroups,
      Dictionary<BeatmapEditorObjectId, List<EventBoxEditorData>> replacementEventBoxes,
      Dictionary<BeatmapEditorObjectId, List<BaseEditorData>> replacementBaseEvents,
      IEnumerable<BeatmapEditorObjectId> selection)
   {
      _eventBoxGroupsClipboardHelper.RemoveEventBoxGroupData(currentGroups, currentEventBoxes, currentBaseEvents);
      _eventBoxGroupsClipboardHelper.InsertEventBoxGroupData(
         replacementGroups,
         replacementEventBoxes,
         replacementBaseEvents);
      _selectionState.Clear();
      _selectionState.AddRange(selection);
      _signalBus.Fire<EventBoxGroupsSelectionStateUpdatedSignal>();
      _signalBus.Fire<BeatmapLevelUpdatedSignal>();
   }

   private static bool EventBoxTargetsMatch(EventBoxEditorData left, EventBoxEditorData right)
   {
      if (left.GetType() != right.GetType()) return false;
      if (left is LightRotationEventBoxEditorData leftRotation
          && ((LightRotationEventBoxEditorData)right).axis != leftRotation.axis)
         return false;
      if (left is LightTranslationEventBoxEditorData leftTranslation
          && ((LightTranslationEventBoxEditorData)right).axis != leftTranslation.axis)
         return false;

      var leftFilter = left.indexFilter;
      var rightFilter = right.indexFilter;
      if (leftFilter.type != rightFilter.type
          || leftFilter.param0 != rightFilter.param0
          || leftFilter.param1 != rightFilter.param1
          || leftFilter.chunks != rightFilter.chunks
          || leftFilter.limit != rightFilter.limit
          || leftFilter.randomType != rightFilter.randomType)
         return false;

      return !leftFilter.randomType.HasFlag(IndexFilter.IndexFilterRandomType.RandomElements)
             || leftFilter.seed == rightFilter.seed;
   }
}
