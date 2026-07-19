using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using HarmonyLib;
using IPA.Utilities;
using UnityEngine;

namespace EditorEnhanced.Patches;

[HarmonyPatch(
   typeof(ModifyHoveredLightTranslationDeltaTranslationCommand),
   nameof(ModifyHoveredLightTranslationDeltaTranslationCommand.GetModifiedEventData))]
public static class ModifyHoveredLightTranslationDeltaTranslationCommandPatches
{
   [HarmonyPostfix]
   private static void RestoreConfiguredTranslationPrecision(
      ModifyHoveredLightTranslationDeltaTranslationCommand __instance,
      ref LightTranslationBaseEditorData __result)
   {
      if (__instance == null
         || __instance._signal == null
         || __instance.beatmapState == null
         || __result == null
         || __instance.originalData == null
         || !ModifyHoveredLightTranslationDeltaTranslationCommand._precisions.ContainsKey(
            __instance.beatmapState.scrollPrecision))
         return;

      var delta =
         ModifyHoveredLightTranslationDeltaTranslationCommand._precisions[__instance.beatmapState.scrollPrecision]
         * Mathf.Sign(__instance._signal.deltaTranslation);
      var translation = Mathf.Round(__instance.originalData.translation * 1_000f + delta * 10f) / 1_000f;
      __result.SetField("translation", translation);
   }
}
