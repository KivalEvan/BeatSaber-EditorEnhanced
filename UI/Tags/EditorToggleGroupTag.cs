using BeatmapEditor3D;
using Tweening;

namespace EditorEnhanced.UI.Tags;

public class EditorToggleGroupBuilder(EditBeatmapViewController ebvc, TimeTweeningManager twm)
{
    public EditorToggleGroupTag CreateNew()
    {
        return new EditorToggleGroupTag(ebvc, twm);
    }
}

public class EditorToggleGroupTag(EditBeatmapViewController ebvc, TimeTweeningManager twm)
{
}