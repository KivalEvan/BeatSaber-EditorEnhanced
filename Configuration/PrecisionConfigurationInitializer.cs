using System;
using System.Collections.Generic;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.Types;
using EditorEnhanced.Misc;
using Zenject;

namespace EditorEnhanced.Configuration;

internal sealed class PrecisionConfigurationInitializer : IInitializable
{
   private readonly PrecisionDefaults _defaults;
   private readonly PluginConfig _pluginConfig;

   public PrecisionConfigurationInitializer(PluginConfig pluginConfig, PrecisionDefaults defaults)
   {
      _pluginConfig = pluginConfig;
      _defaults = defaults;
   }

   public void Initialize()
   {
      var config = _pluginConfig.Precision ?? new PrecisionConfig();
      _pluginConfig.Precision = config;

      config.Color = Normalize("color", config.Color, _defaults.Color);
      config.Rotation = Normalize("rotation", config.Rotation, _defaults.Rotation);
      config.Translation = Normalize("translation", config.Translation, _defaults.Translation);
      config.Fx = Normalize("FX", config.Fx, _defaults.Fx);
      config.Time = Normalize("time", config.Time, _defaults.Time);
      config.Percent = Normalize("percent", config.Percent, _defaults.Percent);

      Apply(LightColorEventHelper._precisions, config.Color);
      Apply(ModifyHoveredLightRotationDeltaRotationCommand._precisions, config.Rotation);
      Apply(ModifyHoveredLightTranslationDeltaTranslationCommand._precisions, config.Translation);
      Apply(ModifyHoveredFloatFxDeltaValueCommand._precisions, config.Fx);
      Apply(CustomPrecisions.TimePrecisionFloat, config.Time);
      Apply(CustomPrecisions.PercentPrecisionFloat, config.Percent);
      for (var i = 0; i < PrecisionDefaults.SupportedTypes.Length; i++)
         CustomPrecisions.PercentPrecisionInt[PrecisionDefaults.SupportedTypes[i]] =
            Math.Max(1, (int)Math.Round(config.Percent[i]));
   }

   private static List<float> Normalize(
      string name,
      IReadOnlyList<float> configured,
      IReadOnlyList<float> defaults)
   {
      var result = new List<float>(PrecisionDefaults.SupportedTypes.Length);
      var repaired = configured != null
         && configured.Count != 0
         && configured.Count != PrecisionDefaults.SupportedTypes.Length;
      for (var i = 0; i < PrecisionDefaults.SupportedTypes.Length; i++)
      {
         var value = configured != null && i < configured.Count ? configured[i] : defaults[i];
         if (!IsValid(value))
         {
            value = defaults[i];
            repaired = true;
         }

         result.Add(value);
      }

      if (repaired) Plugin.Log.Warn($"Normalized invalid {name} precision configuration to four positive values.");
      return result;
   }

   private static void Apply(IDictionary<PrecisionType, float> target, IReadOnlyList<float> values)
   {
      for (var i = 0; i < PrecisionDefaults.SupportedTypes.Length; i++)
         target[PrecisionDefaults.SupportedTypes[i]] = values[i];
   }

   internal static bool IsValid(float value)
   {
      return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
   }
}