using BeatmapEditor3D.Commands;
using SiraUtil.Affinity;

namespace EditorEnhanced.Patches;

public class MoveEventBoxPatches : IAffinity
{
   [AffinityPrefix]
   [AffinityPatch(typeof(MoveEventBoxCommand), nameof(MoveEventBoxCommand.ShouldMergeWith))]
   private bool KeepMovesAsSeparateUndoSteps(ref bool __result)
   {
      __result = false;
      return false;
   }
}