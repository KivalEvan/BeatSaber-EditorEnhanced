using System;
using System.Collections.Generic;
using System.Reflection;
using BeatmapEditor3D;
using EditorEnhanced.UI.Interfaces;
using EditorEnhanced.Utils;
using HMUI;
using IPA.Utilities;
using JetBrains.Annotations;
using TMPro;
using Tweening;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Tags;

public class EditorButtonWithIconBuilder
{
    [Inject] private readonly EditBeatmapViewController _ebvc;
    [Inject] private readonly TimeTweeningManager _twm;

    public EditorButtonWithIconTag CreateNew()
    {
        return new EditorButtonWithIconTag(_ebvc, _twm);
    }
}

public class EditorButtonWithIconTag : IUIButton, IUIText
{
    private readonly EditBeatmapViewController _ebvc;
    private readonly TimeTweeningManager _twm;
    public string ImagePath;

    public EditorButtonWithIconTag(EditBeatmapViewController ebvc, TimeTweeningManager twm)
    {
        _ebvc = ebvc;
        _twm = twm;
    }

    public string[] Aliases => ["editor-button-with-icon", "editor-icon-button"];

    private Button PrefabButton =>
        _ebvc._beatmapEditorExtendedSettingsView._copyDifficultyButton;

    public List<Action> OnClick { get; set; } = [];
    [CanBeNull] public string Text { get; set; }
    public TextAlignmentOptions? TextAlignment { get; set; }
    public bool? RichText { get; set; }
    public float? FontSize { get; set; }
    public FontWeight? FontWeight { get; set; }

    public GameObject CreateObject(Transform parent)
    {
        var button = (NoTransitionsButton)Object.Instantiate(PrefabButton, parent, false);
        button.name = "EEEditorButtonWithTag";
        button.interactable = true;
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
        // image.material = Utilities.ImageResources.NoGlowMat;
        image.rectTransform.SetParent(contentWrapper.transform, false);
        // image.sprite = Utilities.ImageResources.BlankSprite;
        image.preserveAspect = true;
        image.sprite = TextureLoader.LoadSpriteRaw(
            AssetLoader.GetResource(Assembly.GetExecutingAssembly(), ImagePath));
        image.sprite.texture.wrapMode = TextureWrapMode.Clamp;
        btnObject.transform.localScale = new Vector2(64f / 100f, 64f / 100f);

        // var buttonIconImage = go.AddComponent<ButtonIconImage>();
        // buttonIconImage.Image = image;

        var contentSizeFitter = btnObject.AddComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        btnObject.SetActive(true);
        return btnObject;
    }

    public EditorButtonWithIconTag SetImage(string path)
    {
        ImagePath = path;
        return this;
    }
}