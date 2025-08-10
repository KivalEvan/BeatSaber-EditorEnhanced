using BeatmapEditor3D.DataModels;
using SiraUtil.Affinity;

namespace EditorEnhanced.Gizmo.Patches;

public class DebugStatePatches : IAffinity
{
    private readonly DebugState _debugState;

    public DebugStatePatches(DebugState debugState)
    {
        _debugState = debugState;
        _debugState.lightGroupGizmoType = LightGroupGizmoType.None;
    }

    [AffinityPostfix]
    [AffinityPatch(typeof(DebugState), nameof(DebugState.ResetOnBeatmapExit))]
    public void NoGizmoDefault(DebugState __instance)
    {
        __instance.lightGroupGizmoType = LightGroupGizmoType.None;
    }
}