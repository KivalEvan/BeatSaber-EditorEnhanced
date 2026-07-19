using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;

namespace EditorEnhanced.EventBoxes;

public sealed class EventBoxSnapshot
{
   public EventBoxSnapshot(EventBoxEditorData eventBox, List<BaseEditorData> baseEvents)
   {
      EventBox = eventBox;
      BaseEvents = baseEvents;
   }

   public EventBoxEditorData EventBox { get; }
   public List<BaseEditorData> BaseEvents { get; }
}

public sealed class EventBoxGroupSnapshot
{
   private readonly List<EventBoxSnapshot> _eventBoxes;

   public EventBoxGroupSnapshot(BeatmapEditorObjectId groupId, IEnumerable<EventBoxSnapshot> eventBoxes)
   {
      GroupId = groupId;
      _eventBoxes = eventBoxes.ToList();
   }

   public BeatmapEditorObjectId GroupId { get; }
   public IReadOnlyList<EventBoxSnapshot> EventBoxes => _eventBoxes;
   public int Count => _eventBoxes.Count;

   public int IndexOf(BeatmapEditorObjectId eventBoxId)
   {
      for (var i = 0; i < _eventBoxes.Count; i++)
         if (_eventBoxes[i].EventBox.id == eventBoxId)
            return i;

      return -1;
   }

   public bool HasSameOrder(EventBoxGroupSnapshot other)
   {
      return _eventBoxes
         .Select(item => item.EventBox.id)
         .SequenceEqual(other._eventBoxes.Select(item => item.EventBox.id));
   }

   public EventBoxGroupSnapshot WithEventBoxes(IEnumerable<EventBoxSnapshot> eventBoxes)
   {
      return new EventBoxGroupSnapshot(GroupId, eventBoxes);
   }
}
