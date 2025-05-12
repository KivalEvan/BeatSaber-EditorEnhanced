using BeatmapEditor3D;
using Tweening;
using Zenject;

namespace EditorEnhanced.UI.Tags;

public class EditorToggleGroupBuilder
{
    [Inject] private readonly EditBeatmapViewController _ebvc;
    [Inject] private readonly TimeTweeningManager _twm;

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