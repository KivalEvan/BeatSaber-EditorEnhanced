using System.Globalization;
using EditorEnhanced.Utils;
using SiraUtil.Affinity;

namespace EditorEnhanced.Patches;

public class IntInputFieldValidatorPatches : IAffinity
{
   [AffinityPrefix]
   [AffinityPatch(typeof(IntInputFieldValidator), nameof(IntInputFieldValidator.ParseInput))]
   private bool EvaluateMathExpression(ref string input)
   {
      if (int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)) return true;

      if (MathExpressionEvaluator.TryEvaluate(input, out var result)
         && int.TryParse(result, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
         input = result;

      return true;
   }
}