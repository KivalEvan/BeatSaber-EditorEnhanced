using System.Collections.Generic;
using System.Reflection;
using BeatmapEditor3D.Types;
using EditorEnhanced.Misc;
using HarmonyLib;

namespace EditorEnhanced.Patches;

[HarmonyPatch]
public static class PrecisionStepsPatches
{
   private const string RotationMethod = "RotationDegrees";
   private const string TranslationMethod = "Translation";

   [HarmonyPrepare]
   private static bool Prepare() => AccessTools.TypeByName("BeatmapEditor3D.Types.PrecisionSteps") != null;

   private static IEnumerable<MethodBase> TargetMethods()
   {
      var precisionSteps = AccessTools.TypeByName("BeatmapEditor3D.Types.PrecisionSteps");
      if (precisionSteps == null) yield break;

      var rotation = AccessTools.Method(precisionSteps, RotationMethod, [typeof(PrecisionType)]);
      if (rotation != null) yield return rotation;

      var translation = AccessTools.Method(precisionSteps, TranslationMethod, [typeof(PrecisionType)]);
      if (translation != null) yield return translation;
   }

   [HarmonyPrefix]
   private static bool UseConfiguredPrecision(
      MethodBase __originalMethod,
      PrecisionType precision,
      ref float __result)
   {
      var values = __originalMethod.Name == RotationMethod
         ? CustomPrecisions.RotationPrecisionFloat
         : CustomPrecisions.TranslationPrecisionFloat;
      if (!values.TryGetValue(precision, out __result)) return true;
      return false;
   }
}
