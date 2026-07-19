using System;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.Views;
using HarmonyLib;
using IPA.Utilities;

namespace EditorEnhanced.Patches;

[HarmonyPatch(
   typeof(ModifyHoveredLightEventDeltaIntensityCommand),
   nameof(ModifyHoveredLightEventDeltaIntensityCommand.GetModifiedEventData))]
public class ModifyHoveredLightEventDeltaIntensityCommandPatches : IDisposable
{
   private static BeatmapState _beatmapState;
   private readonly BeatmapState _injectedBeatmapState;

   public ModifyHoveredLightEventDeltaIntensityCommandPatches(BeatmapState beatmapState)
   {
      _injectedBeatmapState = beatmapState;
      _beatmapState = beatmapState;
   }

   public void Dispose()
   {
      if (ReferenceEquals(_beatmapState, _injectedBeatmapState)) _beatmapState = null;
   }

   [HarmonyPostfix]
   private static void RestoreConfiguredIntensityPrecision(
      ModifyHoveredLightEventDeltaIntensityCommand __instance,
      BasicEventEditorData originalBasicEventData,
      ref BasicEventEditorData __result)
   {
      if (__instance == null
         || __instance._signal == null
         || _beatmapState == null
         || originalBasicEventData == null
         || __result == null
         || !LightColorEventHelper._precisions.ContainsKey(_beatmapState.scrollPrecision))
         return;

      double floatValue = LightColorEventHelper.IncreaseBrightnessByPrecision(
         originalBasicEventData.floatValue,
         __instance._signal.newDeltaIntensity,
         _beatmapState.scrollPrecision);
      var lightEventsPayload = new LightEventsPayload(originalBasicEventData.value, (float)floatValue);
      __result.SetField("floatValue", lightEventsPayload.intensity);
   }
}
