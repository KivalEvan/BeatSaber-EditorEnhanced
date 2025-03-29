using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.Views;
using SiraUtil.Affinity;

namespace EditorEnhanced.Patches;

public class ModifyHoveredLightEventDeltaIntensityCommandPatches(BeatmapState beatmapState) : IAffinity
{
    [AffinityPrefix]
    [AffinityPatch(typeof(ModifyHoveredLightEventDeltaIntensityCommand),
        nameof(ModifyHoveredLightEventDeltaIntensityCommand.GetModifiedEventData))]
    private bool SomeReallyStupidWayToFix(ModifyHoveredLightEventDeltaIntensityCommand __instance,
        BasicEventEditorData originalBasicEventData,
        ref BasicEventEditorData __result)
    {
        double floatValue = LightColorEventHelper.IncreaseBrightnessByPrecision(originalBasicEventData.floatValue,
            __instance._signal.newDeltaIntensity,
            beatmapState.scrollPrecision);
        var lightEventsPayload = new LightEventsPayload(originalBasicEventData.value, (float)floatValue);
        __result = BasicEventEditorData.CreateNewWithId(originalBasicEventData.id, originalBasicEventData.type,
            originalBasicEventData.beat, lightEventsPayload.ToValue(), lightEventsPayload.ToAltValue());
        return false;
    }
}