using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.Views;
using IPA.Utilities;
using SiraUtil.Affinity;

namespace EditorEnhanced.Patches;

public class ModifyFullLightColorPatches : IAffinity
{
    [AffinityPostfix]
    [AffinityPatch(typeof(ModifyFullLightColorSignal),"", AffinityMethodType.Constructor, null,
        typeof(BeatmapEditorObjectId),
        typeof(BeatmapEditorObjectId),
        typeof(BeatmapEditorObjectId),
        typeof(float),
        typeof(EaseLeadType),
        typeof(EaseCurveType),
        typeof(EnvironmentColorType),
        typeof(float),
        typeof(int),
        typeof(float),
        typeof(bool),
        typeof(bool))]
    private void FixStrobeBrightness(ModifyFullLightColorSignal __instance)
    {
        __instance.SetField("strobeBrightness", __instance.strobeBrightness / 100f);
    }
}