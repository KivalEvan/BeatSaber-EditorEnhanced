using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.Views;
using IPA.Utilities;
using SiraUtil.Affinity;

namespace EditorEnhanced.Patches;

public class LightColorDataViewPatches : IAffinity
{
    [AffinityPrefix]
    [AffinityPatch(typeof(LightColorDataView), nameof(LightColorDataView.UpdateData))]
    private bool FixStrobeBrightness(LightColorDataView __instance)
    {
        var tuple = EaseTypeHelpers.ConvertFromEaseType((EaseType)__instance._easeTypeDropdown.selectedIndex);
        __instance.signalBus.Fire(new ModifyFullLightColorSignal(
            __instance.eventBoxGroupId, 
            __instance.eventBoxId,
            __instance.id, 
            __instance._beatInputFieldValidator.value,
            tuple.Item1,
            tuple.Item2,
            (EnvironmentColorType)(__instance._colorTypeToggleGroup.value - 1),
            __instance._valueInput.value / 100f,
            __instance._strobeFrequencyInput.value,
            __instance._strobeBrightnessInput.value / 100f,
            __instance._strobeFadeToggle.isOn,
            __instance._extensionToggle.isOn));
        return false;
    }
}