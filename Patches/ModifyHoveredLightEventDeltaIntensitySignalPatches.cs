using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.Views;
using SiraUtil.Affinity;

namespace EditorEnhanced.Patches;

public class ModifyHoveredLightEventDeltaIntensityCommandPatches : IAffinity
{
    private readonly BeatmapState _beatmapState;

    public ModifyHoveredLightEventDeltaIntensityCommandPatches(BeatmapState beatmapState)
    {
        _beatmapState = beatmapState;
    }

    [AffinityPrefix]
    [AffinityPatch(typeof(ModifyHoveredLightEventDeltaIntensityCommand),
        nameof(ModifyHoveredLightEventDeltaIntensityCommand.GetModifiedEventData))]
    private bool SomeReallyStupidWayToFix(ModifyHoveredLightEventDeltaIntensityCommand __instance,
        BasicEventEditorData originalBasicEventData,
        ref BasicEventEditorData __result)
    {
        double floatValue = LightColorEventHelper.IncreaseBrightnessByPrecision(originalBasicEventData.floatValue,
            __instance._signal.newDeltaIntensity,
            _beatmapState.scrollPrecision);
        var lightEventsPayload = new LightEventsPayload(originalBasicEventData.value, (float)floatValue);
        __result = BasicEventEditorData.CreateNewWithId(originalBasicEventData.id, originalBasicEventData.type,
            originalBasicEventData.beat, lightEventsPayload.ToValue(), lightEventsPayload.ToAltValue());
        return false;
    }
}