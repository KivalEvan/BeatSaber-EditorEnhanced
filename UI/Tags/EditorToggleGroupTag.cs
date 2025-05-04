using BeatmapEditor3D;
using Tweening;

namespace EditorEnhanced.UI.Tags;

public class EditorToggleGroupBuilder(BeatmapFlowCoordinator bfc, TimeTweeningManager twm)
{
    public EditorToggleGroupTag CreateNew()
    {
        return new EditorToggleGroupTag(bfc, twm);
    }
}

public class EditorToggleGroupTag(BeatmapFlowCoordinator bfc, TimeTweeningManager twm)
{
    
}