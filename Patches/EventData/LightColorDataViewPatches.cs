using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.Views;
using HarmonyLib;

namespace EditorEnhanced.Patches;

[HarmonyPatch(typeof(LightColorDataView), nameof(LightColorDataView.UpdateData))]
public static class LightColorDataViewPatches
{
   [HarmonyPrefix]
   private static bool SubmitNormalizedStrobeBrightness(LightColorDataView __instance)
   {
      if (__instance == null
         || __instance.signalBus == null
         || __instance._easeTypeDropdown == null
         || __instance._beatInputFieldValidator == null
         || __instance._colorTypeToggleGroup == null
         || __instance._valueInput == null
         || __instance._strobeFrequencyInput == null
         || __instance._strobeBrightnessInput == null
         || __instance._strobeFadeToggle == null
         || __instance._extensionToggle == null)
         return true;

      var tuple = EaseTypeHelpers.ConvertFromEaseType((EaseType)__instance._easeTypeDropdown.selectedIndex);
      __instance.signalBus.Fire(
         new ModifyFullLightColorSignal(
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
