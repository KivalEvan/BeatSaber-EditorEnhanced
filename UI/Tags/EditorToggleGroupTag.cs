using BeatmapEditor3D;
using Tweening;

namespace EditorEnhanced.UI.Tags;

public class EditorToggleGroupBuilder
{
    private readonly EditBeatmapViewController _ebvc;
    private readonly TimeTweeningManager _twm;

    public EditorToggleGroupBuilder(EditBeatmapViewController ebvc, TimeTweeningManager twm)
    {
        _ebvc = ebvc;
        _twm = twm;
    }

    public EditorToggleGroupTag CreateNew()
    {
        return new EditorToggleGroupTag(_ebvc, _twm);
    }
}

public class EditorToggleGroupTag
{
    public EditorToggleGroupTag(EditBeatmapViewController ebvc, TimeTweeningManager twm)
    {
    }
}