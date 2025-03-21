using System;
using System.Collections.Generic;

namespace EditorEnhanced.UI.Interfaces;

public interface IUIButton
{
    List<Action> OnClick { get; set; }
}