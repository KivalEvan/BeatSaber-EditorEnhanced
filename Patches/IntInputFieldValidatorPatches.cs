using System;
using System.Globalization;
using BeatmapEditor3D;
using BeatmapEditor3D.Views;
using SiraUtil.Affinity;
using UnityEngine;

namespace EditorEnhanced.Patches;

public class IntInputFieldValidatorPatches : IAffinity
{
    [AffinityPrefix]
    [AffinityPatch(typeof(IntInputFieldValidator), nameof(IntInputFieldValidator.ParseInput))]
    private bool MathEvalInput(IntInputFieldValidator __instance, ref string input)
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