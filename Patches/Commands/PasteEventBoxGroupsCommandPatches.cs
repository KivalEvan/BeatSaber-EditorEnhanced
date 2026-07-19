using System;
using System.Linq;
using BeatmapEditor3D.Commands;
using EditorEnhanced.Managers;
using HarmonyLib;
using IPA.Utilities;
using Random = UnityEngine.Random;

namespace EditorEnhanced.Patches;

[HarmonyPatch(typeof(PasteEventBoxGroupsCommand), nameof(PasteEventBoxGroupsCommand.Redo))]
public class PasteEventBoxGroupsCommandPatches : IDisposable
{
   private static RandomSeedClipboardManager _randomSeedClipboardManager;
   private readonly RandomSeedClipboardManager _injectedRandomSeedClipboardManager;

   public PasteEventBoxGroupsCommandPatches(RandomSeedClipboardManager randomSeedClipboardManager)
   {
      _injectedRandomSeedClipboardManager = randomSeedClipboardManager;
      _randomSeedClipboardManager = randomSeedClipboardManager;
   }

   public void Dispose()
   {
      if (ReferenceEquals(_randomSeedClipboardManager, _injectedRandomSeedClipboardManager))
         _randomSeedClipboardManager = null;
   }

   [HarmonyPrefix]
   private static void RandomizeSeedOnPaste(PasteEventBoxGroupsCommand __instance)
   {
      if (_randomSeedClipboardManager == null || !_randomSeedClipboardManager.RandomOnPaste) return;
      if (__instance?._newEventBoxes == null) return;

      foreach (var boxEditorData in __instance._newEventBoxes.Values.SelectMany(eventBoxEditorData =>
         eventBoxEditorData))
         boxEditorData.indexFilter.SetField(
            "seed",
            _randomSeedClipboardManager.UseClipboard
               ? _randomSeedClipboardManager.Seed
               : Random.Range(int.MinValue, int.MaxValue));
   }
}
