using System;
using System.Collections.Generic;
using System.Linq;
using BeatmapDataLoaderVersion4;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.SerializedData;
using UnityEngine;

namespace EditorEnhanced.Utils;

internal static class IndexFilterHelpers
{
   public static IEnumerable<(int index, int chunkIndex)> GetIndexFilterRange(
      IndexFilterEditorData indexFilter,
      int groupSize)
   {
      var convertedIndexFilter = IndexFilterConverter.Convert(
         LightshowSaver.ConvertIndexFilter(indexFilter),
         groupSize);
      var chunkSize = indexFilter.chunks == 0 ? 1 : Mathf.CeilToInt(groupSize / (float)indexFilter.chunks);
      return convertedIndexFilter.Select((item, localIdx) => (item.element, localIdx / chunkSize));
   }
}