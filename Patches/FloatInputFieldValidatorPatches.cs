using System.Globalization;
using BeatmapEditor3D;
using BeatmapEditor3D.Views;
using SiraUtil.Affinity;
using UnityEngine;

namespace EditorEnhanced.Patches;

public class FloatInputFieldValidatorPatches : IAffinity
{
    [AffinityPrefix]
    [AffinityPatch(typeof(FloatInputFieldValidator), nameof(FloatInputFieldValidator.ValueToString))]
    private bool DoNotFormat(FloatInputFieldValidator __instance, ref string __result)
    {
        __result = __instance.value.ToString(CultureInfo.InvariantCulture);
        return false;
    }
}