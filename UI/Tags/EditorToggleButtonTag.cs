using BeatmapEditor3D;
using Tweening;

namespace EditorEnhanced.UI.Tags;

public class EditorToggleButtonBuilder
{
    private readonly EditBeatmapViewController _ebvc;
    private readonly TimeTweeningManager _twm;

    public EditorToggleButtonBuilder(EditBeatmapViewController ebvc, TimeTweeningManager twm)
    {
        _ebvc = ebvc;
        _twm = twm;
    }

    public EditorToggleButtonTag CreateNew()
    {
        return new EditorToggleButtonTag(_ebvc, _twm);
    }
}

public class EditorToggleButtonTag
{
    public EditorToggleButtonTag(EditBeatmapViewController ebvc, TimeTweeningManager twm)
    {
    }
}