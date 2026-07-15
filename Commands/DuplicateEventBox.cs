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
   public readonly float Value;

   public DuplicateEventBoxSignal(
      BeatmapEditorObjectId eventBoxId,
      bool copyEvent,
      bool randomSeed,
      bool increment,
      float value)
   {
      EventBoxId = eventBoxId;
      CopyEvent = copyEvent;
      RandomSeed = randomSeed;
      Increment = increment;
      Value = value;
   }
}

internal sealed class DuplicateEventBoxCommand : IBeatmapEditorCommandWithHistory
{
   private readonly EventBoxCloneService _cloneService;
   private readonly EventBoxGroupsState _eventBoxGroupsState;
   private readonly EventBoxGroupMutation _mutation;
   private readonly DuplicateEventBoxSignal _signal;
   private BeatmapEditorObjectId _eventBoxGroupId;
   private EventBoxSnapshot _newEventBox;
   private int _newIdx;

   public DuplicateEventBoxCommand(
      DuplicateEventBoxSignal signal,
      EventBoxGroupsState eventBoxGroupsState,
      EventBoxGroupMutation mutation,
      EventBoxCloneService cloneService)
   {
      _signal = signal;
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

      _newIdx = sourceIndex + 1;
      _newEventBox = _cloneService.Clone(
         snapshot.EventBoxes[sourceIndex],
         _signal.CopyEvent,
         _signal.Increment,
         _signal.RandomSeed,
         _signal.Value);
      shouldAddToHistory = true;
      Redo();
   }

   public void Undo()
   {
      _mutation.Remove(_eventBoxGroupId, _newEventBox, _newIdx - 1);
   }

   public void Redo()
   {
      _mutation.Insert(_eventBoxGroupId, _newEventBox, _newIdx, _newIdx);
   }
}