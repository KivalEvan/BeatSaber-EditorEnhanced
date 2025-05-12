using System.Linq;
using BeatmapEditor3D.Commands;
using EditorEnhanced.Managers;
using IPA.Utilities;
using SiraUtil.Affinity;

namespace EditorEnhanced.Patches;

public class PasteEventBoxGroupsCommandPatch(RandomSeedClipboardManager rscm) : IAffinity
{
    [AffinityPrefix]
    [AffinityPatch(typeof(PasteEventBoxGroupsCommand), nameof(PasteEventBoxGroupsCommand.Redo))]
    private void RandomizeSeedOnPaste(PasteEventBoxGroupsCommand __instance)
    {
        if (!rscm.RandomOnPaste) return;

        foreach (var boxEditorData in __instance._newEventBoxes.Values.SelectMany(eventBoxEditorData => eventBoxEditorData))
        {
            boxEditorData.indexFilter.SetField("seed",
                rscm.UseClipboard ? rscm.Seed : UnityEngine.Random.Range(int.MinValue, int.MaxValue));
        }
    }
}