using System;
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

    [AffinityPrefix]
    [AffinityPatch(typeof(FloatInputFieldValidator), nameof(FloatInputFieldValidator.ParseInput))]
    private bool MathEvalInput(FloatInputFieldValidator __instance, ref string input)
    {
        try
        {
            var table = new System.Data.DataTable();
            var computed = table.Compute(input, "");
            input = computed.ToString();
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e);
        }

        return true;
    }
}