using BeatmapEditor3D.Views;
using SiraUtil.Affinity;
using UnityEngine;

namespace EditorEnhanced.Patches;

public class LightEventsPayloadPatches : IAffinity
{
   [AffinityPrefix]
   [AffinityPatch(typeof(LightEventsPayload), nameof(LightEventsPayload.ToAltValue))]
   private bool PreserveIntensityAboveBaseGameLimit(LightEventsPayload __instance, ref float __result)
   {
      if (__instance == null) return true;

      __result = Mathf.Max(0f, __instance.intensity);
      return false;
   }
}