using EditorEnhanced.Configuration;
using EditorEnhanced.UI.Extensions;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Views;

public partial class ConfigurationView : IInitializable
{
   private readonly PluginConfig _config;
   private readonly SignalBus _signalBus;
   private readonly UIBuilder _uiBuilder;
   private readonly EditorViewLocator _viewLocator;

   public ConfigurationView(
      SignalBus signalBus,
      PluginConfig config,
      EditorViewLocator viewLocator,
      UIBuilder uiBuilder)
   {
      _signalBus = signalBus;
      _config = config;
      _viewLocator = viewLocator;
      _uiBuilder = uiBuilder;
   }

   public void Initialize()
   {
      if (!_viewLocator.TryGetRightPanelContent(out var target)
         || !_viewLocator.TryGetNoteBackground(out var noteBackground))
         return;

      var mainContainer = _uiBuilder
         .CreateVerticalLayout()
         .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
         .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetChildAlignment(TextAnchor.UpperLeft)
         .SetPadding(new RectOffset(4, 4, 4, 4))
         .SetSpacing(4)
         .Create(target);
      mainContainer.name = "EditorEnhancedView";

      BuildGizmoSection(mainContainer.transform, noteBackground);
      BuildPrecisionSection(mainContainer.transform, noteBackground);

      if (!_viewLocator.TryRegisterPanel("Editor Enhanced", mainContainer)) Object.Destroy(mainContainer);
   }
}
