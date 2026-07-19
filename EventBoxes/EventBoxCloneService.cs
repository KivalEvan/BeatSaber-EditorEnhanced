using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using IPA.Utilities;
using UnityEngine;

namespace EditorEnhanced.EventBoxes;

public sealed class EventBoxCloneService
{
   public EventBoxSnapshot Clone(
      EventBoxSnapshot source,
      bool copyBaseEvents,
      bool incrementFilter,
      bool randomizeSeed,
      float valueOffset,
      int maxId)
   {
      var eventBox = EventBoxGroupsClipboardHelper.CopyEventBoxEditorDataWithoutId(source.EventBox);
      if (incrementFilter) IncrementFilter(eventBox, maxId);
      if (randomizeSeed) RandomizeSeed(eventBox);

      var baseEvents = copyBaseEvents
         ? source.BaseEvents.Select(item => CloneBaseEvent(item, valueOffset)).ToList()
         : new List<BaseEditorData>();

      return new EventBoxSnapshot(eventBox, baseEvents);
   }

   private static BaseEditorData CloneBaseEvent(BaseEditorData source, float valueOffset)
   {
      var clone = EventBoxGroupsClipboardHelper.CopyBaseEditorDataWithoutId(source);
      switch (clone)
      {
         case LightColorBaseEditorData color:
            color.SetField("brightness", color.brightness + valueOffset / 100f);
            break;
         case LightRotationBaseEditorData rotation:
            rotation.SetField("rotation", rotation.rotation + valueOffset);
            break;
         case LightTranslationBaseEditorData translation:
            translation.SetField("translation", translation.translation + valueOffset / 100f);
            break;
         case FloatFxBaseEditorData fx:
            fx.SetField("value", fx.value + valueOffset / 100f);
            break;
      }

      return clone;
   }

   private static void IncrementFilter(EventBoxEditorData eventBox, int maxId)
   {
      if (eventBox.indexFilter.type == IndexFilterEditorData.IndexFilterType.Division)
         eventBox.indexFilter.SetField("param1", IncrementId(eventBox.indexFilter.param1, maxId));
      else
         eventBox.indexFilter.SetField("param0", IncrementId(eventBox.indexFilter.param0, maxId));
   }

   private static int IncrementId(int id, int maxId)
   {
      return id >= maxId ? maxId : id + 1;
   }

   private static void RandomizeSeed(EventBoxEditorData eventBox)
   {
      if (eventBox.indexFilter.randomType.HasFlag(IndexFilter.IndexFilterRandomType.RandomElements))
         eventBox.indexFilter.SetField("seed", Random.Range(int.MinValue, int.MaxValue));
   }
}
