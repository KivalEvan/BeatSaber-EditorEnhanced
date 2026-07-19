using System.Globalization;
using EditorEnhanced.Utils;
using HarmonyLib;

namespace EditorEnhanced.Patches;

[HarmonyPatch(typeof(IntInputFieldValidator), nameof(IntInputFieldValidator.ParseInput))]
internal static class IntInputFieldValidatorPatches
{
   [HarmonyPrefix]
   private static void EvaluateMathExpression(ref string input)
   {
      if (int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)) return;

      if (MathExpressionEvaluator.TryEvaluate(input, out var result)
          && int.TryParse(result, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
         input = result;
   }
}
