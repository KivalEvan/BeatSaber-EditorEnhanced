using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using IPA.Utilities;
using SiraUtil.Affinity;
using UnityEngine;

namespace EditorEnhanced.Patches;

public class ModifyHoveredLightTranslationDeltaTranslationCommandPatches : IAffinity
{
    [AffinityPostfix]
    [AffinityPatch(typeof(ModifyHoveredLightTranslationDeltaTranslationCommand),
        nameof(ModifyHoveredLightTranslationDeltaTranslationCommand.GetModifiedEventData))]
    public void WhyDoIHaveToReimplementThis(ModifyHoveredLightTranslationDeltaTranslationCommand __instance,
        ref LightTranslationBaseEditorData __result)
    {
        var delta =
            ModifyHoveredLightTranslationDeltaTranslationCommand._precisions[__instance.beatmapState.scrollPrecision] *
            Mathf.Sign(__instance._signal.deltaTranslation);
        float? translation = Mathf.Round(__instance.originalData.translation * 1_000f + delta * 10f) / 1_000f;
        __result.SetField("translation", translation.Value);
    }
}