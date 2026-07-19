using BeatmapEditor3D.DataModels;
using HarmonyLib;

namespace EditorEnhanced.Patches;

[HarmonyPatch(typeof(FxEventBoxEditorData), nameof(FxEventBoxEditorData.CopyWithoutId))]
internal static class FxEventBoxEditorDataPatches
{
   [HarmonyPrefix]
   private static bool CopyIndexFilterInsteadOfSharingIt(FxEventBoxEditorData original, ref FxEventBoxEditorData __result)
   {
      if (original?.indexFilter == null) return true;

      __result = FxEventBoxEditorData.CreateNew(
         IndexFilterEditorData.Copy(original.indexFilter),
         original.beatDistributionParamType,
         original.beatDistributionParam,
         original.vfxDistributionParamType,
         original.vfxDistributionParam,
         original.vfxDistributionShouldAffectFirstBaseEvent,
         original.vfxDistributionEaseType);
      return false;
   }
}
