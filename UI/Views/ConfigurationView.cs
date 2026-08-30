using EditorEnhanced.Configuration;
using EditorEnhanced.UI.Components;
using EditorEnhanced.UI.Extensions;
using HMUI;
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
      if (!_viewLocator.TryGetRightPanelContent(out var content)
         || !_viewLocator.TryGetNoteBackground(out var noteBackground))
         return;

      var panelRoot = CreatePanelRoot(content);
      var mainContainer = _uiBuilder
         .CreateVerticalLayout()
         .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
         .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetChildAlignment(TextAnchor.UpperLeft)
         .SetPadding(new RectOffset(4, 4, 4, 4))
         .SetSpacing(4)
         .Create(panelRoot.transform);
      mainContainer.name = "EditorEnhancedContent";
      ConfigureInnerContent((RectTransform)mainContainer.transform);

      BuildGizmoSection(mainContainer.transform, noteBackground);
      BuildMotionPathSection(mainContainer.transform, noteBackground);
      BuildPrecisionSection(mainContainer.transform, noteBackground);

      if (!_viewLocator.TryRegisterPanel("Editor Enhanced", panelRoot))
      {
         Object.Destroy(panelRoot);
         return;
      }

      var layoutInitializer = content.gameObject.AddComponent<ConfigurationPanelLayoutInitializer>();
      layoutInitializer.Configure(
         panelRoot,
         (RectTransform)mainContainer.transform,
         content,
         content.GetComponentInParent<ScrollView>());
   }

   private static GameObject CreatePanelRoot(RectTransform content)
   {
      var panelRoot = new GameObject("EditorEnhancedView", typeof(RectTransform))
      {
         layer = content.gameObject.layer
      };
      panelRoot.SetActive(false);

      var rectTransform = (RectTransform)panelRoot.transform;
      rectTransform.SetParent(content, false);
      rectTransform.anchorMin = new Vector2(0f, 1f);
      rectTransform.anchorMax = new Vector2(0f, 1f);
      rectTransform.pivot = new Vector2(0f, 1f);
      rectTransform.anchoredPosition = Vector2.zero;
      var width = content.rect.width > 0f ? content.rect.width : content.sizeDelta.x;
      if (width > 0f) rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

      var layoutElement = panelRoot.AddComponent<LayoutElement>();
      layoutElement.minHeight = 0f;
      layoutElement.flexibleHeight = 0f;
      return panelRoot;
   }

   private static void ConfigureInnerContent(RectTransform content)
   {
      content.anchorMin = new Vector2(0f, 1f);
      content.anchorMax = new Vector2(1f, 1f);
      content.pivot = new Vector2(0f, 1f);
      content.anchoredPosition = Vector2.zero;
      content.sizeDelta = Vector2.zero;
   }
}
