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

public class EditorCheckboxBuilder
{
    private readonly EditBeatmapNavigationViewController _ebnvc;
    private readonly TimeTweeningManager _twm;

    public EditorCheckboxBuilder(EditBeatmapNavigationViewController ebnvc, TimeTweeningManager twm)
    {
        _ebnvc = ebnvc;
        _twm = twm;
    }

    public EditorCheckboxTag CreateNew()
    {
        return new EditorCheckboxTag(_ebnvc, _twm);
    }
}

public class EditorCheckboxTag : IUIToggle, IUIText
{
    private readonly EditBeatmapNavigationViewController _ebnvc;
    private readonly TimeTweeningManager _twm;

    public EditorCheckboxTag(EditBeatmapNavigationViewController ebnvc, TimeTweeningManager twm)
    {
        _ebnvc = ebnvc;
        _twm = twm;
    }

    public string[] Aliases => ["editor-checkbox"];

    private Toggle PrefabToggle => _ebnvc._eventBoxGroupsToolbarView._extensionToggle;

    public string Text { get; set; }
    public TextAlignmentOptions? TextAlignment { get; set; }
    public bool? RichText { get; set; }
    public float? FontSize { get; set; }
    public FontWeight? FontWeight { get; set; }

    [CanBeNull] public List<Action<bool>> OnValueChange { get; set; } = [];
    public bool? Bool { get; set; }
    public float? Size { get; set; }

    public GameObject CreateObject(Transform parent)
    {
        var toggle = (NoTransitionsToggle)Object.Instantiate(PrefabToggle, parent, false);
        toggle.name = "EEEditorCheckbox";
        toggle.interactable = true;
        toggle.isOn = Bool ?? toggle.isOn;
        OnValueChange?.ForEach(x => toggle.onValueChanged.AddListener(x.Invoke));

        var comp = toggle.GetComponent<NoTransitionToggleSelectableStateController>();
        ((SelectableStateController)comp).SetField("_tweeningManager", _twm);

        var toggleObject = toggle.gameObject;
        toggleObject.SetActive(false);
        var horizontalLayoutGroup = toggleObject.AddComponent<HorizontalLayoutGroup>();
        horizontalLayoutGroup.childControlWidth = false;
        horizontalLayoutGroup.childControlHeight = false;
        horizontalLayoutGroup.spacing = 2f;
        horizontalLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
        // var layoutElement = toggleObject.AddComponent<LayoutElement>();
        // layoutElement.flexibleWidth = 1f;

        var contentWrapper = new GameObject("ContentWrapper");
        contentWrapper.transform.SetParent(toggleObject.transform, false);
        var stackLayoutGroup = contentWrapper.AddComponent<StackLayoutGroup>();
        // stackLayoutGroup.padding = new RectOffset(12, 12, 6, 6);

        var labelObject = toggle.transform.Find("BeatmapEditorLabel").gameObject;
        labelObject.transform.SetParent(contentWrapper.transform, false);
        var tmp = labelObject.GetComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignment ?? TextAlignmentOptions.Left;
        tmp.text = Text ?? "Default Text";
        tmp.fontSize = FontSize ?? 12f;
        tmp.fontWeight = FontWeight ?? tmp.fontWeight;
        tmp.richText = true;

        var contentSizeFitter = contentWrapper.AddComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        var background = toggle.transform.Find("Background4px").gameObject;
        background.GetComponent<RectTransform>().sizeDelta = new Vector2(Size ?? 12f, Size ?? 12f);

        toggleObject.SetActive(true);
        return toggleObject;
    }
}