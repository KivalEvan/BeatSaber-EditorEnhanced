using System;
using System.Data;
using SiraUtil.Affinity;

namespace EditorEnhanced.Patches;

public class IntInputFieldValidatorPatches : IAffinity
{
    [AffinityPrefix]
    [AffinityPatch(typeof(IntInputFieldValidator), nameof(IntInputFieldValidator.ParseInput))]
    private bool MathEvalInput(IntInputFieldValidator __instance, ref string input)
    {
        try
        {
            var table = new DataTable();
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