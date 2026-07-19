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
      float valueOffset)
   {
      var eventBox = EventBoxGroupsClipboardHelper.CopyEventBoxEditorDataWithoutId(source.EventBox);
      if (incrementFilter) IncrementFilter(eventBox);
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

   private static void IncrementFilter(EventBoxEditorData eventBox)
   {
      if (eventBox.indexFilter.type == IndexFilterEditorData.IndexFilterType.Division)
         eventBox.indexFilter.SetField("param1", eventBox.indexFilter.param1 + 1);
      else
         eventBox.indexFilter.SetField("param0", eventBox.indexFilter.param0 + 1);
   }

   private static void RandomizeSeed(EventBoxEditorData eventBox)
   {
      if (eventBox.indexFilter.randomType.HasFlag(IndexFilter.IndexFilterRandomType.RandomElements))
         eventBox.indexFilter.SetField("seed", Random.Range(int.MinValue, int.MaxValue));
   }
}
