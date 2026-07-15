using System;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using EditorEnhanced.EventBoxes;

namespace EditorEnhanced.Commands;

public class ReorderEventBoxSignal
{
   public readonly int CurrentIndex;
   public readonly int MoveToIndex;

   public ReorderEventBoxSignal(int currentIndex, int moveToIndex)
   {
      CurrentIndex = currentIndex;
      MoveToIndex = moveToIndex;
   }
}

internal sealed class ReorderEventBoxCommand : IBeatmapEditorCommandWithHistory
{
   private readonly EventBoxGroupsState _eventBoxGroupsState;
   private readonly EventBoxGroupMutation _mutation;
   private readonly ReorderEventBoxSignal _signal;
   private int _newIdx;
   private EventBoxGroupSnapshot _newSnapshot;
   private EventBoxGroupSnapshot _previousSnapshot;

   public ReorderEventBoxCommand(
      ReorderEventBoxSignal signal,
      EventBoxGroupsState eventBoxGroupsState,
      EventBoxGroupMutation mutation)
   {
      _signal = signal;
      _eventBoxGroupsState = eventBoxGroupsState;
      _mutation = mutation;
   }

   public bool shouldAddToHistory { get; private set; }

   public void Execute()
   {
      if (_signal.CurrentIndex == _signal.MoveToIndex) return;

      var context = _eventBoxGroupsState.eventBoxGroupContext;
      if (context == null) return;

      var previousSnapshot = _mutation.Capture(context.id);
      if (_signal.CurrentIndex < 0 || _signal.CurrentIndex >= previousSnapshot.Count) return;

      var newIdx = Math.Clamp(_signal.MoveToIndex, 0, previousSnapshot.Count - 1);

      if (newIdx == _signal.CurrentIndex) return;

      var reordered = previousSnapshot.EventBoxes.ToList();
      var selectedEventBox = reordered[_signal.CurrentIndex];
      reordered.RemoveAt(_signal.CurrentIndex);
      reordered.Insert(newIdx, selectedEventBox);

      _newIdx = newIdx;
      _previousSnapshot = previousSnapshot;
      _newSnapshot = previousSnapshot.WithEventBoxes(reordered);
      shouldAddToHistory = true;
      Redo();
   }

   public void Undo()
   {
      _mutation.Replace(_newSnapshot, _previousSnapshot, _newIdx);
   }

   public void Redo()
   {
      _mutation.Replace(_previousSnapshot, _newSnapshot, _newIdx);
   }
}