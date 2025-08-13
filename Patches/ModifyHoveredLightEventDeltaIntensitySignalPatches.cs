using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.Views;
using IPA.Utilities;
using SiraUtil.Affinity;

namespace EditorEnhanced.Patches;

public class ModifyHoveredLightEventDeltaIntensityCommandPatches : IAffinity
{
   private readonly BeatmapState _beatmapState;

   public ModifyHoveredLightEventDeltaIntensityCommandPatches(BeatmapState beatmapState)
   {
      _beatmapState = beatmapState;
   }

   [AffinityPostfix]
   [AffinityPatch(
      typeof(ModifyHoveredLightEventDeltaIntensityCommand),
      nameof(ModifyHoveredLightEventDeltaIntensityCommand.GetModifiedEventData))]
   private void SomeReallyStupidWayToFix(
      ModifyHoveredLightEventDeltaIntensityCommand __instance,
      BasicEventEditorData originalBasicEventData,
      ref BasicEventEditorData __result)
   {
      double floatValue = LightColorEventHelper.IncreaseBrightnessByPrecision(
         originalBasicEventData.floatValue,
         __instance._signal.newDeltaIntensity,
         _beatmapState.scrollPrecision);
      var lightEventsPayload = new LightEventsPayload(originalBasicEventData.value, (float)floatValue);
      __result.SetField("floatValue", lightEventsPayload.intensity);
   }
}