using System.Globalization;
using BeatmapEditor3D;
using EditorEnhanced.Utils;
using SiraUtil.Affinity;

namespace EditorEnhanced.Patches;

public class FloatInputFieldValidatorPatches : IAffinity
{
   [AffinityPrefix]
   [AffinityPatch(typeof(FloatInputFieldValidator), nameof(FloatInputFieldValidator.ParseInput))]
   private bool EvaluateMathExpression(ref string input)
   {
      if (float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out _)) return true;

      if (MathExpressionEvaluator.TryEvaluate(input, out var result)
         && float.TryParse(result, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
         input = result;

      return true;
   }
}