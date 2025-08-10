using System;
using System.Collections.Generic;

namespace EditorEnhanced.UI.Interfaces;

public interface IUISlider
{
    float? Value { get; set; }
    float? MinValue { get; set; }
    float? MaxValue { get; set; }
    bool? WholeNumber { get; set; }
    List<Action<float>> OnValueChange { get; set; }
}