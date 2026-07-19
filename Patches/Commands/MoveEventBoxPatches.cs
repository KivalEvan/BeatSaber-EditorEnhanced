using BeatmapEditor3D.Commands;
using HarmonyLib;

namespace EditorEnhanced.Patches;

[HarmonyPatch(typeof(MoveEventBoxCommand), nameof(MoveEventBoxCommand.ShouldMergeWith))]
public static class MoveEventBoxPatches
{
   [HarmonyPrefix]
   private static bool KeepMovesAsSeparateUndoSteps(ref bool __result)
   {
      __result = false;
      return false;
   }
}
