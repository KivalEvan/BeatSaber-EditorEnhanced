using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using EditorEnhanced.Misc;
using HarmonyLib;
using IPA.Utilities;
using UnityEngine;

namespace EditorEnhanced.Patches;

[HarmonyPatch(
   typeof(ModifyHoveredLightRotationDeltaRotationCommand),
   nameof(ModifyHoveredLightRotationDeltaRotationCommand.GetModifiedEventData))]
public static class ModifyHoveredLightRotationDeltaRotationCommandPatches
{
   [HarmonyPostfix]
   private static void RestoreConfiguredRotationPrecision(
      ModifyHoveredLightRotationDeltaRotationCommand __instance,
      ref LightRotationBaseEditorData __result)
   {
      if (__instance == null
         || __instance._signal == null
         || __instance.beatmapState == null
         || __result == null
         || __instance.originalData == null
         || !CustomPrecisions.RotationPrecisionFloat.TryGetValue(
            __instance.beatmapState.scrollPrecision,
            out var precision))
         return;

      var rotation = Mathf.Repeat(
         __instance.originalData.rotation + precision * Mathf.Sign(__instance._signal.deltaRotation),
         360f);
      __result.SetField("rotation", rotation);
   }
}
