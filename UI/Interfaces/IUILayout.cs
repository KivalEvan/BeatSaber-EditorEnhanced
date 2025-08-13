using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace EditorEnhanced.UI.Interfaces;

public interface IUILayout
{
   float? Spacing { get; set; }
   [CanBeNull] RectOffset Padding { get; set; }
   ContentSizeFitter.FitMode? VerticalFit { get; set; }
   ContentSizeFitter.FitMode? HorizontalFit { get; set; }
   TextAnchor? ChildAlignment { get; set; }
   bool? ChildControlWidth { get; set; }
   bool? ChildControlHeight { get; set; }
   bool? ChildScaleWidth { get; set; }
   bool? ChildScaleHeight { get; set; }
   bool? ChildForceExpandWidth { get; set; }
   bool? ChildForceExpandHeight { get; set; }

   float? FlexibleWidth { get; set; }
   float? FlexibleHeight { get; set; }
   float? PreferredWidth { get; set; }
   float? PreferredHeight { get; set; }
}