using BeatmapEditor3D.Commands;
using SiraUtil.Affinity;

namespace EditorEnhanced.Patches;

public class MoveEventBoxPatches : IAffinity
{
   [AffinityPrefix]
   [AffinityPatch(typeof(MoveEventBoxCommand), nameof(MoveEventBoxCommand.ShouldMergeWith))]
   private bool NoMerging(ref bool __result)
   {
      __result = false;
      return false;
   }
}