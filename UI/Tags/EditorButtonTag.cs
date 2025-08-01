using System;
using System.Collections.Generic;
using BeatmapEditor3D;
using EditorEnhanced.UI.Interfaces;
using HMUI;
using IPA.Utilities;
using JetBrains.Annotations;
using TMPro;
using Tweening;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Tags;

public class EditorButtonBuilder
{
    private readonly EditBeatmapViewController _ebvc;
    private readonly TimeTweeningManager _twm;

    public EditorButtonBuilder(EditBeatmapViewController ebvc, TimeTweeningManager twm)
    {
        _ebvc = ebvc;
        _twm = twm;
    }

    public EditorButtonTag CreateNew()
    {
        return new EditorButtonTag(_ebvc, _twm);
    }
}

public class EditorButtonTag : IUIButton, IUIText, IUILayout
{
    private readonly EditBeatmapViewController _ebvc;
    private readonly TimeTweeningManager _twm;

    public EditorButtonTag(EditBeatmapViewController ebvc, TimeTweeningManager twm)
    {
        _ebvc = ebvc;
        _twm = twm;
    }

    public string[] Aliases => ["editor-button"];

    private Button PrefabButton => _ebvc._beatmapEditorExtendedSettingsView._copyDifficultyButton;

    [CanBeNull] public List<Action> OnClick { get; set; } = [];
    [CanBeNull] public string Text { get; set; }
    public TextAlignmentOptions? TextAlignment { get; set; }
    public bool? RichText { get; set; }
    public float? FontSize { get; set; }
    public FontWeight? FontWeight { get; set; }

    public GameObject CreateObject(Transform parent)
    {
        var button = (NoTransitionsButton)Object.Instantiate(PrefabButton, parent, false);
        button.name = "EEEditorButton";
        button.interactable = true;
        OnClick.ForEach(x => button.onClick.AddListener(x.Invoke));

        var comp = button.GetComponent<NoTransitionButtonSelectableStateController>();
        ((SelectableStateController)comp).SetField("_tweeningManager", _twm);

        var btnObject = button.gameObject;
        btnObject.SetActive(false);
        var stackLayoutGroup = btnObject.AddComponent<StackLayoutGroup>();
        var layoutElement = btnObject.AddComponent<LayoutElement>();
        ((RectTransform)layoutElement.transform).sizeDelta = Size ?? ((RectTransform)layoutElement.transform).sizeDelta;
        layoutElement.flexibleWidth = 1f;
        layoutElement.flexibleHeight = 1f;
        stackLayoutGroup.padding = Padding ?? stackLayoutGroup.padding;

        var contentWrapper = new GameObject("ContentWrapper");
        contentWrapper.transform.SetParent(btnObject.transform, false);
        layoutElement = contentWrapper.AddComponent<LayoutElement>();
        ((RectTransform)layoutElement.transform).sizeDelta = Size ?? ((RectTransform)layoutElement.transform).sizeDelta;
        layoutElement.flexibleWidth = 1f;
        layoutElement.flexibleHeight = 1f;
        if (Size.HasValue)
        {
            layoutElement.preferredWidth = Size.Value.x;
            layoutElement.preferredHeight = Size.Value.y;
        }
        
        stackLayoutGroup = contentWrapper.AddComponent<StackLayoutGroup>();
        stackLayoutGroup.childForceExpandWidth = ChildForceExpandWidth ?? stackLayoutGroup.childForceExpandWidth;
        stackLayoutGroup.childForceExpandHeight = ChildForceExpandHeight ?? stackLayoutGroup.childForceExpandHeight;
        stackLayoutGroup.padding = Padding ?? new RectOffset(12, 12, 6, 6);


        var labelObject = button.transform.Find("BeatmapEditorLabel").gameObject;
        labelObject.transform.SetParent(contentWrapper.transform, false);
        var tmp = labelObject.GetComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignment ?? TextAlignmentOptions.Center;
        tmp.text = Text ?? "Default Text";
        tmp.fontSize = FontSize ?? 12;
        tmp.fontWeight = FontWeight ?? tmp.fontWeight;
        tmp.richText = true;

        var contentSizeFitter = btnObject.AddComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        btnObject.SetActive(true);
        return btnObject;
    }

    public EditorButtonTag SetSize(Vector2 size)
    {
        Size = size;
        return this;
    }

    public Vector2? Size { get; set; }
    public float? Spacing { get; set; }
    public RectOffset Padding { get; set; }
    public ContentSizeFitter.FitMode? VerticalFit { get; set; }
    public ContentSizeFitter.FitMode? HorizontalFit { get; set; }
    public TextAnchor? ChildAlignment { get; set; }
    public bool? ChildControlWidth { get; set; }
    public bool? ChildControlHeight { get; set; }
    public bool? ChildScaleWidth { get; set; }
    public bool? ChildScaleHeight { get; set; }
    public bool? ChildForceExpandWidth { get; set; }
    public bool? ChildForceExpandHeight { get; set; }
    public float? FlexibleWidth { get; set; }
    public float? FlexibleHeight { get; set; }
}