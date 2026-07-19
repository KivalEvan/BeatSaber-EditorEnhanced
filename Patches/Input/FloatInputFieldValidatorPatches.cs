using System.Globalization;
using BeatmapEditor3D;
using EditorEnhanced.Utils;
using HarmonyLib;

namespace EditorEnhanced.Patches;

[HarmonyPatch(typeof(FloatInputFieldValidator), nameof(FloatInputFieldValidator.ParseInput))]
internal static class FloatInputFieldValidatorPatches
{
   [HarmonyPrefix]
   private static void EvaluateMathExpression(ref string input)
   {
      if (float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out _)) return;

      if (MathExpressionEvaluator.TryEvaluate(input, out var result)
          && float.TryParse(result, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
         input = result;
   }
}
