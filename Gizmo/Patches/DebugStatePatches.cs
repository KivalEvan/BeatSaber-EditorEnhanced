using BeatmapEditor3D.DataModels;
using HarmonyLib;

namespace EditorEnhanced.Gizmo.Patches;

[HarmonyPatch(typeof(DebugState), nameof(DebugState.ResetOnBeatmapExit))]
public class DebugStatePatches
{
   public DebugStatePatches(DebugState debugState)
   {
      debugState.lightGroupGizmoType = LightGroupGizmoType.None;
   }

   [HarmonyPostfix]
   private static void DisableBuiltInLightGroupGizmoAfterReset(DebugState __instance)
   {
      if (__instance != null) __instance.lightGroupGizmoType = LightGroupGizmoType.None;
   }
}
