using System;
using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;

namespace EditorEnhanced.Utils;

internal static class EventBoxDurationHelpers
{
   public static IReadOnlyDictionary<int, float> GetNextEventBoxGroupDurationById(
      BeatmapEventBoxGroupsDataModel dataModel,
      EventBoxGroupEditorData currentGroup,
      EventBoxEditorData currentEventBox)
   {
      if (dataModel == null) throw new ArgumentNullException(nameof(dataModel));
      if (currentGroup == null) throw new ArgumentNullException(nameof(currentGroup));
      if (currentEventBox == null) throw new ArgumentNullException(nameof(currentEventBox));

      if (!dataModel.TryGetGroupSizeByEventBoxGroupId(currentGroup.groupId, out var groupSize))
         return new Dictionary<int, float>();

      var unresolvedIds = IndexFilterHelpers
         .GetIndexFilterRange(currentEventBox.indexFilter, groupSize)
         .Select(item => item.index)
         .Where(id => id >= 0 && id < groupSize)
         .ToHashSet();
      var durations = new Dictionary<int, float>();

      foreach (var nextGroup in dataModel
         .GetAllEventBoxGroups()
         .Where(group =>
            group.groupId == currentGroup.groupId
            && group.type == currentGroup.type
            && group.beat > currentGroup.beat)
         .OrderBy(group => group.beat))
      {
         foreach (var eventBox in dataModel
            .GetEventBoxesByEventBoxGroupId(nextGroup.id)
            .Where(eventBox => HasSameAxis(currentEventBox, eventBox)))
         {
            var baseEvents = dataModel.GetBaseEventsListByEventBoxId(eventBox.id).ToArray();
            if (baseEvents.Length == 0) continue;

            var firstEventBeat = baseEvents.Min(baseEvent => baseEvent.beat);
            foreach (var id in IndexFilterHelpers
               .GetIndexFilterRange(eventBox.indexFilter, groupSize)
               .Select(item => item.index)
               .Where(unresolvedIds.Contains)
               .Distinct()
               .ToArray())
            {
               durations[id] = nextGroup.beat + firstEventBeat - currentGroup.beat;
               unresolvedIds.Remove(id);
            }
         }

         if (unresolvedIds.Count == 0) break;
      }

      return durations;
   }

   private static bool HasSameAxis(EventBoxEditorData current, EventBoxEditorData candidate)
   {
      return (current, candidate) switch
      {
         (LightRotationEventBoxEditorData currentRotation, LightRotationEventBoxEditorData candidateRotation) =>
            currentRotation.axis == candidateRotation.axis,
         (LightTranslationEventBoxEditorData currentTranslation,
            LightTranslationEventBoxEditorData candidateTranslation) =>
            currentTranslation.axis == candidateTranslation.axis,
         (LightRotationEventBoxEditorData, _) or (LightTranslationEventBoxEditorData, _) => false,
         _ => true
      };
   }
}