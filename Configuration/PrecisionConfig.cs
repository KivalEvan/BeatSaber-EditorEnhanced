using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using EditorEnhanced.Misc;
using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;

namespace EditorEnhanced.Configuration;

public class PrecisionConfig
{
   [UseConverter(typeof(ListConverter<float>))]
   public virtual List<float> Color { get; set; } = LightColorEventHelper._precisions.Values.ToList();

   [UseConverter(typeof(ListConverter<float>))]
   public virtual List<float> Rotation { get; set; } =
      ModifyHoveredLightRotationDeltaRotationCommand._precisions.Values.ToList();

   [UseConverter(typeof(ListConverter<float>))]
   public virtual List<float> Translation { get; set; } =
      ModifyHoveredLightTranslationDeltaTranslationCommand._precisions.Values.ToList();

   [UseConverter(typeof(ListConverter<float>))]
   public virtual List<float> Fx { get; set; } = ModifyHoveredFloatFxDeltaValueCommand._precisions.Values.ToList();
   
   [UseConverter(typeof(ListConverter<float>))]
   public virtual List<float> Time { get; set; } = CustomPrecisions.TimePrecisionFloat.Values.ToList();
   
   [UseConverter(typeof(ListConverter<float>))]
   public virtual List<float> Percent { get; set; } = CustomPrecisions.PercentPrecisionFloat.Values.ToList();
}