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

public class EditorLabelTag : BSMLTag
{
    public override string[] Aliases => ["editor-label"];

    private GameObject _textPrefab;

    public virtual GameObject PrefabText
    {
        get
        {
            var label = GameObject.Find(
                "/Wrapper/ViewControllers/EditBeatmapViewController/StatusBarView/StatusBarControls/BasicControlsWrapper/ZenModeToggle/BeatmapEditorLabel");
            return label;
        }
    }

    public override GameObject CreateObject(Transform parent)
    {
        if (_textPrefab == null)
            _textPrefab = PrefabText;
        var container = new GameObject("BSMLEditorLabelContainer");
        container.transform.SetParent(parent, false);
        container.AddComponent<LayoutElement>();
        container.AddComponent<StackLayoutGroup>();
        
        var labelObject = Object.Instantiate(_textPrefab, container.transform, false);
        labelObject.name = "BSMLEditorLabel";

        var component = labelObject.GetComponent<TextMeshProUGUI>();
        component.alignment = TextAlignmentOptions.Center;
        component.text = "Default Text";
        component.fontSize = 12;
        component.richText = true;

        return container;
    }
}