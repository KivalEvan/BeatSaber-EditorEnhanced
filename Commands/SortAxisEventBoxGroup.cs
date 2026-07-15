using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using EditorEnhanced.EventBoxes;

namespace EditorEnhanced.Commands;

public class SortAxisEventBoxGroupSignal
{
}

internal sealed class SortAxisEventBoxGroupCommand : IBeatmapEditorCommandWithHistory
{
   private readonly EventBoxGroupsState _eventBoxGroupsState;
   private readonly EventBoxGroupMutation _mutation;
   private EventBoxGroupSnapshot _newSnapshot;
   private EventBoxGroupSnapshot _previousSnapshot;

   public SortAxisEventBoxGroupCommand(
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
         previousSnapshot.EventBoxes.OrderBy(item =>
         {
            return item.EventBox switch
            {
               LightTranslationEventBoxEditorData ltebed => ltebed.axis,
               LightRotationEventBoxEditorData lrebed => lrebed.axis,
               _ => LightAxis.X
            };
         }));

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