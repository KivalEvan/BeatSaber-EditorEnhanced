using BeatmapEditor3D;
using BeatmapEditor3D.Views;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Tags;
using HMUI;
using IPA.Utilities;
using TMPro;
using Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace EditorEnhanced.UI;

public class EditorButtonTextTag : BSMLTag
{
    public override string[] Aliases => ["editor-button"];

    private Button _buttonPrefab;

    public virtual Button PrefabButton
    {
        get
        {
            var view = GameObject.Find("/Wrapper/ViewControllers/EditBeatmapViewController/StatusBarView");
            return view == null ? null : view.GetComponent<StatusBarView>()._playbackSpeedResetButton;
        }
    }

    public override GameObject CreateObject(Transform parent)
    {
        if (_buttonPrefab == null)
            _buttonPrefab = PrefabButton;
        var button = (NoTransitionsButton)Object.Instantiate(_buttonPrefab, parent, false);
        button.name = "BSMLEditorButton";
        button.interactable = true;

        Object.Destroy(button.GetComponent<NoTransitionButtonSelectableStateController>());

        var btnObject = button.gameObject;
        btnObject.SetActive(false);
        var externalComponents = btnObject.AddComponent<ExternalComponents>();
        var stackLayoutGroup = btnObject.AddComponent<StackLayoutGroup>();
        var layoutElement = btnObject.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = 0f;

        var contentWrapper = new GameObject("ContentWrapper");
        contentWrapper.transform.SetParent(btnObject.transform, false);
        stackLayoutGroup = contentWrapper.AddComponent<StackLayoutGroup>();
        stackLayoutGroup.padding = new RectOffset(12, 12, 6, 6);
        layoutElement = contentWrapper.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = 0f;
        
        var labelObject = button.transform.Find("BeatmapEditorLabel").gameObject;
        labelObject.transform.SetParent(contentWrapper.transform, false);
        var component = labelObject.GetComponent<TextMeshProUGUI>();
        component.alignment = TextAlignmentOptions.Center;
        component.text = "Default Text";
        component.fontSize = 12;
        component.richText = true;
        externalComponents.Components.Add(component);
        
        var contentSizeFitter = btnObject.AddComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        var componentInChildren = button.GetComponentInChildren<LayoutGroup>();
        if (componentInChildren != null)
            externalComponents.Components.Add(componentInChildren);

        btnObject.SetActive(true);
        return btnObject;
    }
}