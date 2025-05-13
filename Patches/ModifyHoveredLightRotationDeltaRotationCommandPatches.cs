using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.Types;
using IPA.Utilities;
using SiraUtil.Affinity;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.Patches;

public class ModifyHoveredLightRotationDeltaRotationCommandPatches : IAffinity, IInitializable
{
    public void Initialize()
    {
        // ModifyHoveredLightRotationDeltaRotationCommand._precisions[PrecisionType.Ultra] = 1f; // 1f
        ModifyHoveredLightRotationDeltaRotationCommand._precisions[PrecisionType.High] = 2.5f; // 5f
        // ModifyHoveredLightRotationDeltaRotationCommand._precisions[PrecisionType.Standard] = 15f; // 15f
        // ModifyHoveredLightRotationDeltaRotationCommand._precisions[PrecisionType.Low] = 30f; // 30f
    }

    [AffinityPostfix]
    [AffinityPatch(typeof(ModifyHoveredLightRotationDeltaRotationCommand),
        nameof(ModifyHoveredLightRotationDeltaRotationCommand.GetModifiedEventData))]
    public void WhyDoIHaveToReimplementThis(ModifyHoveredLightRotationDeltaRotationCommand __instance,
        ref LightRotationBaseEditorData __result)
    {
        var delta =
            ModifyHoveredLightRotationDeltaRotationCommand._precisions[__instance.beatmapState.scrollPrecision] *
            Mathf.Sign(__instance._signal.deltaRotation);
        float? rotation = Mathf.Repeat(__instance.originalData.rotation + delta, 360f);
        __result.SetField("rotation", rotation.Value);
    }
}