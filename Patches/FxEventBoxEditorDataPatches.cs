using BeatmapEditor3D.DataModels;
using SiraUtil.Affinity;

namespace EditorEnhanced.Patches;

public class FxEventBoxEditorDataPatches : IAffinity
{
   [AffinityPrefix]
   [AffinityPatch(typeof(FxEventBoxEditorData), nameof(FxEventBoxEditorData.CopyWithoutId))]
   private bool FixIndexFilterCopy(FxEventBoxEditorData original, ref FxEventBoxEditorData __result)
   {
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