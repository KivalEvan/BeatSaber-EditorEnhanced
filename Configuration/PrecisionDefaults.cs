using System;
using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.Types;
using EditorEnhanced.Misc;

namespace EditorEnhanced.Configuration;

public sealed class PrecisionDefaults
{
   public static readonly PrecisionType[] SupportedTypes =
   [
      PrecisionType.Ultra, PrecisionType.High, PrecisionType.Standard, PrecisionType.Low
   ];

   public PrecisionDefaults()
   {
      Color = Capture(LightColorEventHelper._precisions);
      Rotation = Capture(ModifyHoveredLightRotationDeltaRotationCommand._precisions);
      Translation = Capture(ModifyHoveredLightTranslationDeltaTranslationCommand._precisions);
      Fx = Capture(ModifyHoveredFloatFxDeltaValueCommand._precisions);
      Time = Capture(CustomPrecisions.TimePrecisionFloat);
      Percent = Capture(CustomPrecisions.PercentPrecisionFloat);
   }

   public IReadOnlyList<float> Color { get; }
   public IReadOnlyList<float> Rotation { get; }
   public IReadOnlyList<float> Translation { get; }
   public IReadOnlyList<float> Fx { get; }
   public IReadOnlyList<float> Time { get; }
   public IReadOnlyList<float> Percent { get; }

   public static int GetIndex(PrecisionType type)
   {
      return Array.IndexOf(SupportedTypes, type);
   }

   private static IReadOnlyList<float> Capture(IReadOnlyDictionary<PrecisionType, float> source)
   {
      return SupportedTypes.Select(type => source.TryGetValue(type, out var value) ? value : 1f).ToArray();
   }
}
