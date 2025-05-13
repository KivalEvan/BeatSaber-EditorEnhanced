using System.Linq;
using BeatmapEditor3D.Commands;
using EditorEnhanced.Managers;
using IPA.Utilities;
using SiraUtil.Affinity;
using UnityEngine;

namespace EditorEnhanced.Patches;

public class PasteEventBoxGroupsCommandPatches : IAffinity
{
    private readonly RandomSeedClipboardManager _rscm;

    public PasteEventBoxGroupsCommandPatches(RandomSeedClipboardManager rscm)
    {
        _rscm = rscm;
    }

    [AffinityPrefix]
    [AffinityPatch(typeof(PasteEventBoxGroupsCommand), nameof(PasteEventBoxGroupsCommand.Redo))]
    private void RandomizeSeedOnPaste(PasteEventBoxGroupsCommand __instance)
    {
        if (!_rscm.RandomOnPaste) return;

        foreach (var boxEditorData in __instance._newEventBoxes.Values.SelectMany(eventBoxEditorData =>
                     eventBoxEditorData))
            boxEditorData.indexFilter.SetField("seed",
                _rscm.UseClipboard ? _rscm.Seed : Random.Range(int.MinValue, int.MaxValue));
    }
}