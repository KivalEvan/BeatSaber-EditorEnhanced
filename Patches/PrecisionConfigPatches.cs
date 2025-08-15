using System;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.Types;
using EditorEnhanced.Configuration;
using EditorEnhanced.Misc;
using Zenject;

namespace EditorEnhanced.Patches;

public class PrecisionConfigPatches : IInitializable
{
   private readonly PluginConfig _pluginConfig;

   public PrecisionConfigPatches(PluginConfig pluginConfig)
   {
      _pluginConfig = pluginConfig;
   }

   public void Initialize()
   {
      for (var i = 0; i < _pluginConfig.Precision.Color.Count; i++)
         LightColorEventHelper._precisions[(PrecisionType)i] = _pluginConfig.Precision.Color[i];
      for (var i = 0; i < _pluginConfig.Precision.Rotation.Count; i++)
         ModifyHoveredLightRotationDeltaRotationCommand._precisions[(PrecisionType)i] =
            _pluginConfig.Precision.Rotation[i];
      for (var i = 0; i < _pluginConfig.Precision.Translation.Count; i++)
         ModifyHoveredLightTranslationDeltaTranslationCommand._precisions[(PrecisionType)i] =
            _pluginConfig.Precision.Translation[i];
      for (var i = 0; i < _pluginConfig.Precision.Fx.Count; i++)
         ModifyHoveredFloatFxDeltaValueCommand._precisions[(PrecisionType)i] = _pluginConfig.Precision.Fx[i];
      for (var i = 0; i < _pluginConfig.Precision.Time.Count; i++)
         CustomPrecisions.TimePrecisionFloat[(PrecisionType)i] = _pluginConfig.Precision.Time[i];
      for (var i = 0; i < _pluginConfig.Precision.Percent.Count; i++)
      {
         CustomPrecisions.PercentPrecisionFloat[(PrecisionType)i] = _pluginConfig.Precision.Percent[i];
         CustomPrecisions.PercentPrecisionInt[(PrecisionType)i] = (int)Math.Round(_pluginConfig.Precision.Percent[i]);
      }
   }
}