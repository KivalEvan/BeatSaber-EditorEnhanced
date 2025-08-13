using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.Views;
using IPA.Utilities;
using SiraUtil.Affinity;

namespace EditorEnhanced.Patches;

public class EditObjectViewPatches : IAffinity
{
    [AffinityPrefix]
    [AffinityPatch(typeof(EditObjectView), nameof(EditObjectView.SetData), AffinityMethodType.Normal, null,
        typeof(BaseEditorData))]
    private bool FixStrobeBrightness(EditObjectView __instance, BaseEditorData baseData)
    {
        switch (baseData)
        {
            case LightColorBaseEditorData color:
                __instance.SetData(color);
                return false;
            case LightRotationBaseEditorData rotation:
                __instance.SetData(rotation);
                return false;
            case LightTranslationBaseEditorData translation:
                __instance.SetData(translation);
                return false;
            case FloatFxBaseEditorData fx:
                __instance.SetData(fx);
                return false;
            default:
                __instance.SetNoData();
                return false;
        }
    }
}