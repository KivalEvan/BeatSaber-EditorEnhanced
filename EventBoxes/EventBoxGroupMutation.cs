using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.LevelEditor;
using Zenject;

namespace EditorEnhanced.EventBoxes;

internal sealed class EventBoxGroupMutation
{
   private readonly BeatmapEventBoxGroupsDataModel _dataModel;
   private readonly SignalBus _signalBus;

   public EventBoxGroupMutation(BeatmapEventBoxGroupsDataModel dataModel, SignalBus signalBus)
   {
      _dataModel = dataModel;
      _signalBus = signalBus;
   }

   public EventBoxGroupSnapshot Capture(BeatmapEditorObjectId groupId)
   {
      return new EventBoxGroupSnapshot(
         groupId,
         _dataModel
            .GetEventBoxesByEventBoxGroupId(groupId)
            .Select(Capture));
   }

   public EventBoxSnapshot Capture(EventBoxEditorData eventBox)
   {
      return new EventBoxSnapshot(
         eventBox,
         _dataModel.GetBaseEventsListByEventBoxId(eventBox.id).ToList());
   }

   public void Replace(
      EventBoxGroupSnapshot current,
      EventBoxGroupSnapshot replacement,
      int selectedIndex)
   {
      RemoveAll(current);
      InsertAll(replacement);
      NotifyChanged(selectedIndex);
   }

   public void Insert(
      BeatmapEditorObjectId groupId,
      EventBoxSnapshot eventBox,
      int index,
      int selectedIndex)
   {
      _dataModel.InsertEventBox(groupId, eventBox.EventBox, index);
      _dataModel.InsertBaseEditorDataList(eventBox.EventBox.id, eventBox.BaseEvents);
      NotifyChanged(selectedIndex);
   }

   public void Remove(
      BeatmapEditorObjectId groupId,
      EventBoxSnapshot eventBox,
      int selectedIndex)
   {
      _dataModel.RemoveBaseEditorDataList(eventBox.EventBox.id, eventBox.BaseEvents);
      _dataModel.RemoveEventBox(groupId, eventBox.EventBox);
      NotifyChanged(selectedIndex);
   }

   private void RemoveAll(EventBoxGroupSnapshot snapshot)
   {
      foreach (var item in snapshot.EventBoxes)
      {
         _dataModel.RemoveBaseEditorDataList(item.EventBox.id, item.BaseEvents);
         _dataModel.RemoveEventBox(snapshot.GroupId, item.EventBox);
      }
   }

   private void InsertAll(EventBoxGroupSnapshot snapshot)
   {
      foreach (var item in snapshot.EventBoxes)
      {
         _dataModel.InsertEventBox(snapshot.GroupId, item.EventBox);
         _dataModel.InsertBaseEditorDataList(item.EventBox.id, item.BaseEvents);
      }
   }

   private void NotifyChanged(int selectedIndex)
   {
      _signalBus.Fire(new EventBoxesUpdatedSignal(selectedIndex));
      _signalBus.Fire<BeatmapLevelUpdatedSignal>();
   }
}