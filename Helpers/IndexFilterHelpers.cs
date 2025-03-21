using System.Collections.Generic;
using BeatmapDataLoaderVersion4;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.SerializedData;

namespace EditorEnhanced.Helpers;

internal static class IndexFilterHelpers
{
    public static List<int> GetIndexFilterRange(IndexFilterEditorData filter, int size)
    {
        var idxFil = IndexFilterConverter.Convert(LightshowSaver.ConvertIndexFilter(filter), size);
        var l = new List<int>();
        using var enumerator = idxFil.GetEnumerator();
        while (enumerator.MoveNext())
            l.Add(enumerator.Current.element);
        return l;
    }
}