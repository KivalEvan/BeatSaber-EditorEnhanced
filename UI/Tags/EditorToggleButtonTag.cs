using BeatmapEditor3D;
using Tweening;

namespace EditorEnhanced.UI.Tags;

public class EditorToggleButtonBuilder(BeatmapFlowCoordinator bfc, TimeTweeningManager twm)
{
    public EditorToggleButtonTag CreateNew()
    {
        return new EditorToggleButtonTag(bfc, twm);
    }
}

public class EditorToggleButtonTag(BeatmapFlowCoordinator bfc, TimeTweeningManager twm)
{
    
}