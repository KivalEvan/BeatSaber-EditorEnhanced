using BeatmapEditor3D;
using Tweening;
using Zenject;

namespace EditorEnhanced.UI.Tags;

public class EditorToggleButtonBuilder
{
    [Inject] private readonly EditBeatmapViewController _ebvc;
    [Inject] private readonly TimeTweeningManager _twm;

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