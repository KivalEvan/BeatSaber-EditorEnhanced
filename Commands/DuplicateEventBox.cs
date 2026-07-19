using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using EditorEnhanced.EventBoxes;

namespace EditorEnhanced.Commands;

public class DuplicateEventBoxSignal
{
   public readonly bool CopyEvent;
   public readonly BeatmapEditorObjectId EventBoxId;
   public readonly bool Increment;
   public readonly bool RandomSeed;
   public readonly bool ReplaceOriginal;
   public readonly float Value;

   public DuplicateEventBoxSignal(
      BeatmapEditorObjectId eventBoxId,
      bool copyEvent,
      bool randomSeed,
      bool increment,
      float value,
      bool replaceOriginal)
   {
      EventBoxId = eventBoxId;
      CopyEvent = copyEvent;
      RandomSeed = randomSeed;
      Increment = increment;
      Value = value;
      ReplaceOriginal = replaceOriginal;
   }
}

public sealed class DuplicateEventBoxCommand : IBeatmapEditorCommandWithHistory
{
   private readonly EventBoxCloneService _cloneService;
   private readonly BeatmapEventBoxGroupsDataModel _dataModel;
   private readonly EventBoxGroupsState _eventBoxGroupsState;
   private readonly EventBoxGroupMutation _mutation;
   private readonly DuplicateEventBoxSignal _signal;
   private BeatmapEditorObjectId _eventBoxGroupId;
   private EventBoxSnapshot _newEventBox;
   private int _newIdx;
   private EventBoxSnapshot _sourceEventBox;

   public DuplicateEventBoxCommand(
      DuplicateEventBoxSignal signal,
      BeatmapEventBoxGroupsDataModel dataModel,
      EventBoxGroupsState eventBoxGroupsState,
      EventBoxGroupMutation mutation,
      EventBoxCloneService cloneService)
   {
      _signal = signal;
      _dataModel = dataModel;
      _eventBoxGroupsState = eventBoxGroupsState;
      _mutation = mutation;
      _cloneService = cloneService;
   }

   public bool shouldAddToHistory { get; private set; }

   public void Execute()
   {
      var context = _eventBoxGroupsState.eventBoxGroupContext;
      if (context == null) return;

      _eventBoxGroupId = context.id;
      var snapshot = _mutation.Capture(_eventBoxGroupId);
      var sourceIndex = snapshot.IndexOf(_signal.EventBoxId);
      if (sourceIndex < 0) return;

      _sourceEventBox = snapshot.EventBoxes[sourceIndex];
      _newIdx = sourceIndex + (_signal.ReplaceOriginal ? 0 : 1);
      var maxId = int.MaxValue;
      if (_signal.Increment)
      {
         if (!_dataModel.TryGetGroupSizeByEventBoxGroupId(context.groupId, out var groupSize) || groupSize <= 0)
            return;
         maxId = groupSize - 1;
      }
      _newEventBox = _cloneService.Clone(
         _sourceEventBox,
         _signal.CopyEvent,
         _signal.Increment,
         _signal.RandomSeed,
         _signal.Value,
         maxId);
      shouldAddToHistory = true;
      Redo();
   }

   public void Undo()
   {
      if (_signal.ReplaceOriginal)
         _mutation.Replace(_eventBoxGroupId, _newEventBox, _sourceEventBox, _newIdx);
      else
         _mutation.Remove(_eventBoxGroupId, _newEventBox, _newIdx - 1);
   }

   public void Redo()
   {
      if (_signal.ReplaceOriginal)
         _mutation.Replace(_eventBoxGroupId, _sourceEventBox, _newEventBox, _newIdx);
      else
         _mutation.Insert(_eventBoxGroupId, _newEventBox, _newIdx, _newIdx);
   }
}
