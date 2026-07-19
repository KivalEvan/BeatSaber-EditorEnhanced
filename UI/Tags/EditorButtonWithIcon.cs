using System;
using System.Collections.Generic;
using System.Reflection;
using EditorEnhanced.UI.Interfaces;
using EditorEnhanced.Utils;
using HMUI;
using IPA.Utilities;
using Tweening;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Tags;

public class EditorButtonWithIconTag : IEditorTag, IUIButton
{
   private readonly Button _prefabButton;
   private readonly TimeTweeningManager _twm;
   private readonly UIButtonAudioFeedback _audioFeedback;
   public string ImagePath;

   public EditorButtonWithIconTag(Button prefabButton, TimeTweeningManager twm)
      : this(prefabButton, twm, new UIButtonAudioFeedback())
   {
   }

   internal EditorButtonWithIconTag(Button prefabButton, TimeTweeningManager twm, UIButtonAudioFeedback audioFeedback)
   {
      _prefabButton = prefabButton;
      _twm = twm;
      _audioFeedback = audioFeedback;
   }

   public string Name { get; set; } = "EEEditorButtonWithIcon";

   public GameObject Create(Transform parent)
   {
      var button = (NoTransitionsButton)Object.Instantiate(_prefabButton, parent, false);
      button.name = Name;
      button.interactable = true;
      if (AudioFeedback) _audioFeedback.Attach(button);
      OnClick.ForEach(x => button.onClick.AddListener(x.Invoke));

      var comp = button.GetComponent<NoTransitionButtonSelectableStateController>();
      ((SelectableStateController)comp).SetField("_tweeningManager", _twm);

      Object.Destroy(button.transform.Find("BeatmapEditorLabel").gameObject);

      var btnObject = button.gameObject;
      btnObject.SetActive(false);
      var stackLayoutGroup = btnObject.AddComponent<StackLayoutGroup>();
      var layoutElement = btnObject.AddComponent<LayoutElement>();
      layoutElement.flexibleWidth = 1f;

      var contentWrapper = new GameObject("ContentWrapper");
      contentWrapper.transform.SetParent(btnObject.transform, false);
      stackLayoutGroup = contentWrapper.AddComponent<StackLayoutGroup>();
      stackLayoutGroup.padding = new RectOffset(12, 12, 6, 6);

      var image = (Image)new GameObject("Icon").AddComponent<ImageView>();
      image.rectTransform.SetParent(contentWrapper.transform, false);
      image.preserveAspect = true;
      image.sprite = TextureLoader.LoadSpriteRaw(AssetLoader.GetResource(Assembly.GetExecutingAssembly(), ImagePath));
      image.sprite.texture.wrapMode = TextureWrapMode.Clamp;
      btnObject.transform.localScale = new Vector2(64f / 100f, 64f / 100f);

      var contentSizeFitter = btnObject.AddComponent<ContentSizeFitter>();
      contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
      contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

      btnObject.SetActive(true);
      return btnObject;
   }

   public bool AudioFeedback { get; set; }
   public List<Action> OnClick { get; set; } = [];

   public EditorButtonWithIconTag SetImage(string path)
   {
      ImagePath = path;
      return this;
   }
}
