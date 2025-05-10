using BeatmapEditor3D;
using Tweening;

namespace EditorEnhanced.UI.Tags;

public class EditorToggleButtonBuilder(EditBeatmapViewController ebvc, TimeTweeningManager twm)
{
    public EditorToggleButtonTag CreateNew()
    {
        return new EditorToggleButtonTag(ebvc, twm);
    }
}

public class EditorToggleButtonTag(EditBeatmapViewController ebvc, TimeTweeningManager twm)
{
}