using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using IPA.Utilities;
using SiraUtil.Affinity;
using UnityEngine;

namespace EditorEnhanced.Patches;

public class ModifyHoveredLightRotationDeltaRotationCommandPatches : IAffinity
{
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