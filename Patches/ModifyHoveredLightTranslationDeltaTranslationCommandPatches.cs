using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.Types;
using IPA.Utilities;
using SiraUtil.Affinity;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.Patches;

public class ModifyHoveredLightTranslationDeltaTranslationCommandPatches : IAffinity, IInitializable
{
    public void Initialize()
    {
        // ModifyHoveredLightTranslationDeltaTranslationCommand._precisions[PrecisionType.Ultra] = 0.1f; // 1f
        // ModifyHoveredLightTranslationDeltaTranslationCommand._precisions[PrecisionType.High] = 1f; // 2.5f
        ModifyHoveredLightTranslationDeltaTranslationCommand._precisions[PrecisionType.Standard] = 10f; // 5f
        ModifyHoveredLightTranslationDeltaTranslationCommand._precisions[PrecisionType.Low] = 100f; // 10f
    }

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