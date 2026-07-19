using System;
using System.Collections.Generic;

namespace EditorEnhanced.UI.Interfaces;

public interface IUIButton
{
   bool AudioFeedback { get; set; }
   List<Action> OnClick { get; set; }
}
