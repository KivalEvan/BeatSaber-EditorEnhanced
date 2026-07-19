using System.Collections.Generic;
using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;

namespace EditorEnhanced.Configuration;

public class PrecisionConfig
{
   [UseConverter(typeof(ListConverter<float>))]
   public virtual List<float> Color { get; set; } = [];

   [UseConverter(typeof(ListConverter<float>))]
   public virtual List<float> Rotation { get; set; } = [];

   [UseConverter(typeof(ListConverter<float>))]
   public virtual List<float> Translation { get; set; } = [];

   [UseConverter(typeof(ListConverter<float>))]
   public virtual List<float> Fx { get; set; } = [];

   [UseConverter(typeof(ListConverter<float>))]
   public virtual List<float> Time { get; set; } = [];

   [UseConverter(typeof(ListConverter<float>))]
   public virtual List<float> Percent { get; set; } = [];
}
