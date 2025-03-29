using System;
using System.Collections.Generic;
using BeatmapEditor3D;
using EditorEnhanced.UI.Interfaces;
using HMUI;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Tags;

public class EditorButtonWithIconTag(BeatmapFlowCoordinator bfc) : IUIButton, IUIText
{
    public string[] Aliases => ["editor-button-with-icon", "editor-icon-button"];
    
    private Button PrefabButton => bfc._editBeatmapViewController._beatmapEditorExtendedSettingsView._copyDifficultyButton;

    public GameObject CreateObject(Transform parent)
    {
        var button = (NoTransitionsButton)Object.Instantiate(PrefabButton, parent, false);
        button.name = "EEEditorButtonWithTag";
        button.interactable = true;

        Object.Destroy(button.GetComponent<NoTransitionButtonSelectableStateController>());
        Object.Destroy(button.transform.Find("BeatmapEditorLabel").gameObject);

        var go = button.gameObject;
        go.SetActive(false);
        var layoutElement = go.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1f;
        
        var image = (Image) new GameObject("Icon").AddComponent<ImageView>();
        // image.material = Utilities.ImageResources.NoGlowMat;
        image.rectTransform.SetParent(button.transform, false);
        image.rectTransform.sizeDelta = new Vector2(20f, 20f);
        // image.sprite = Utilities.ImageResources.BlankSprite;
        image.preserveAspect = true;
        
        // var buttonIconImage = go.AddComponent<ButtonIconImage>();
        // buttonIconImage.Image = image;

        var contentSizeFitter = go.AddComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        go.SetActive(true);
        return go;
    }

    public List<Action> OnClick { get; set; }
    [CanBeNull] public string Text { get; set; }
    public TextAlignmentOptions? TextAlignment { get; set; }
    public bool? RichText { get; set; }
    public float? FontSize { get; set; }
    public FontWeight? FontWeight { get; set; }
}