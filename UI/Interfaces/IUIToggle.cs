using System;
using System.Collections.Generic;

namespace EditorEnhanced.UI.Interfaces;

public interface IUIToggle
{
    List<Action<bool>> OnValueChange { get; set; }

    bool? Bool { get; set; }
    float? Size { get; set; }
}