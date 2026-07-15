using BeatmapEditor3D;
using BeatmapEditor3D.Views;
using EditorEnhanced.UI.Extensions;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.UI.Views;

internal sealed class OffsetDurationDistributionView : IInitializable
{
   private readonly UIBuilder _uiBuilder;
   private readonly EditorViewLocator _viewLocator;

   private EventBoxView _ebv;

   public OffsetDurationDistributionView(
      EditorViewLocator viewLocator,
      UIBuilder uiBuilder)
   {
      _viewLocator = viewLocator;
      _uiBuilder = uiBuilder;
   }

   public void Initialize()
   {
      if (!_viewLocator.TryGetEventBoxView(out _ebv)) return;

      var buttonTag = _uiBuilder.CreateButton()
         .SetFontSize(16);

      buttonTag
         .SetText("-0.001")
         .SetOnClick(OffsetNegative)
         .Create(_ebv._beatDistributionInput.transform.parent)
         .transform.localPosition = new Vector3(160f, 25f, 0f);
      buttonTag
         .SetText("+0.001")
         .SetOnClick(OffsetPositive)
         .Create(_ebv._beatDistributionInput.transform.parent)
         .transform.localPosition = new Vector3(240f, 25f, 0f);
   }

   private void OffsetNegative()
   {
      _ebv._beatDistributionInput.SetValue(_ebv._eventBox.beatDistributionParam - 0.001f);
   }

   private void OffsetPositive()
   {
      _ebv._beatDistributionInput.SetValue(_ebv._eventBox.beatDistributionParam + 0.001f);
   }
}
