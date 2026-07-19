using BeatmapEditor3D.Views;
using HarmonyLib;
using UnityEngine;

namespace EditorEnhanced.Patches;

[HarmonyPatch(typeof(LightEventsPayload), nameof(LightEventsPayload.ToAltValue))]
internal static class LightEventsPayloadPatches
{
   [HarmonyPrefix]
   private static bool PreserveIntensityAboveBaseGameLimit(LightEventsPayload __instance, ref float __result)
   {
      if (__instance == null) return true;

      __result = Mathf.Max(0f, __instance.intensity);
      return false;
   }
}
