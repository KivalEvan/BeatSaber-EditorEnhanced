using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using EditorEnhanced.EventBoxes;

namespace EditorEnhanced.Commands;

public class SortIdEventBoxGroupSignal
{
}

public sealed class SortIdEventBoxGroupCommand : IBeatmapEditorCommandWithHistory
{
   private readonly EventBoxGroupsState _eventBoxGroupsState;
   private readonly EventBoxGroupMutation _mutation;
   private EventBoxGroupSnapshot _newSnapshot;
   private EventBoxGroupSnapshot _previousSnapshot;

   public SortIdEventBoxGroupCommand(
      EventBoxGroupsState eventBoxGroupsState,
      EventBoxGroupMutation mutation)
   {
      _eventBoxGroupsState = eventBoxGroupsState;
      _mutation = mutation;
   }

   public bool shouldAddToHistory { get; private set; }

   public void Execute()
   {
      var context = _eventBoxGroupsState.eventBoxGroupContext;
      if (context == null) return;

      var previousSnapshot = _mutation.Capture(context.id);
      if (previousSnapshot.Count == 0) return;

      var newSnapshot = previousSnapshot.WithEventBoxes(
         previousSnapshot
            .EventBoxes
            .OrderByDescending(item =>
               item.EventBox.indexFilter.type == IndexFilterEditorData.IndexFilterType.Division
                  ? item.EventBox.indexFilter.param0
                  : item.EventBox.indexFilter.param1)
            .ThenBy(item =>
               item.EventBox.indexFilter.type == IndexFilterEditorData.IndexFilterType.Division
                  ? item.EventBox.indexFilter.param1
                  : item.EventBox.indexFilter.param0));

      if (newSnapshot.HasSameOrder(previousSnapshot)) return;

      _previousSnapshot = previousSnapshot;
      _newSnapshot = newSnapshot;
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
