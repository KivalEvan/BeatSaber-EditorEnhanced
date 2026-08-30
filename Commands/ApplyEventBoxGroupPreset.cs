using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using EditorEnhanced.EventBoxes;

namespace EditorEnhanced.Commands;

public sealed class ApplyEventBoxGroupPresetSignal
{
   public ApplyEventBoxGroupPresetSignal(IReadOnlyList<EventBoxEditorData> eventBoxes)
   {
      EventBoxes = eventBoxes;
   }

   public IReadOnlyList<EventBoxEditorData> EventBoxes { get; }
}

public sealed class ApplyEventBoxGroupPresetCommand : IBeatmapEditorCommandWithHistory
{
   private readonly EventBoxGroupsState _eventBoxGroupsState;
   private readonly EventBoxGroupMutation _mutation;
   private readonly ApplyEventBoxGroupPresetSignal _signal;
   private EventBoxGroupSnapshot _newSnapshot;
   private EventBoxGroupSnapshot _previousSnapshot;

   public ApplyEventBoxGroupPresetCommand(
      ApplyEventBoxGroupPresetSignal signal,
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
      var context = _eventBoxGroupsState.eventBoxGroupContext;
      if (context == null || _signal.EventBoxes == null) return;

      _previousSnapshot = _mutation.Capture(context.id);
      _newSnapshot = new EventBoxGroupSnapshot(
         context.id,
         _signal.EventBoxes.Select(item => new EventBoxSnapshot(item, new List<BaseEditorData>())));
      shouldAddToHistory = true;
      Redo();
   }

   public void Undo()
   {
      _mutation.Replace(_newSnapshot, _previousSnapshot, 0);
   }

   public void Redo()
   {
      _mutation.Replace(_previousSnapshot, _newSnapshot, 0);
   }
}
