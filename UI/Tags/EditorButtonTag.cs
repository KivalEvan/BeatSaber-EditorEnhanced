using System;
using System.Collections.Generic;
using BeatmapEditor3D.Views;
using EditorEnhanced.UI.Interfaces;
using HMUI;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Tags;

public class EditorButtonTag : IUIButton, IUIText
{
    public string[] Aliases => ["editor-button"];
    
    private Button _buttonPrefab;

    private static Button PrefabButton
    {
        get
        {
            var view = GameObject.Find("/Wrapper/ViewControllers/EditBeatmapViewController/StatusBarView");
            return view == null ? null : view.GetComponent<StatusBarView>()._playbackSpeedResetButton;
        }
    }

    public GameObject CreateObject(Transform parent)
    {
        if (_buttonPrefab == null)
            _buttonPrefab = PrefabButton;
        var button = (NoTransitionsButton)Object.Instantiate(_buttonPrefab, parent, false);
        button.name = "EEEditorButton";
        button.interactable = true;
        OnClick.ForEach(x => button.onClick.AddListener(x.Invoke));

        Object.Destroy(button.GetComponent<NoTransitionButtonSelectableStateController>());

        var btnObject = button.gameObject;
        btnObject.SetActive(false);
        var stackLayoutGroup = btnObject.AddComponent<StackLayoutGroup>();
        var layoutElement = btnObject.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1f;

        var contentWrapper = new GameObject("ContentWrapper");
        contentWrapper.transform.SetParent(btnObject.transform, false);
        stackLayoutGroup = contentWrapper.AddComponent<StackLayoutGroup>();
        stackLayoutGroup.padding = new RectOffset(12, 12, 6, 6);
        layoutElement = contentWrapper.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1f;
        
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

    [CanBeNull] public List<Action> OnClick { get; set; } = [];
    [CanBeNull] public string Text { get; set; }
    public TextAlignmentOptions? TextAlignment { get; set; }
    public bool? RichText { get; set; }
    public float? FontSize { get; set; }
    public FontWeight? FontWeight { get; set; }
}