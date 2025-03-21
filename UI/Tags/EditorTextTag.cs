using EditorEnhanced.UI.Interfaces;
using HMUI;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace EditorEnhanced.UI.Tags;

public class EditorTextTag : IUIText
{
    public string[] Aliases => ["editor-text", "editor-label"];
    
    private GameObject _textPrefab;
    private static GameObject PrefabText
    {
        get
        {
            var label = GameObject.Find(
                "/Wrapper/ViewControllers/EditBeatmapViewController/StatusBarView/StatusBarControls/BasicControlsWrapper/ZenModeToggle/BeatmapEditorLabel");
            return label;
        }
    }
    
    public GameObject CreateObject(Transform parent)
    {
        if (_textPrefab == null)
            _textPrefab = PrefabText;
        
        var go = new GameObject("EEEditorText")
        {
            layer = 5
        };
        go.SetActive(false);
        go.transform.SetParent(parent, false);

        var ctmp = go.AddComponent<CurvedTextMeshPro>();
        ctmp.color = Color.white;
        if (_textPrefab != null)
        {
            var prefabComponent = _textPrefab.GetComponent<CurvedTextMeshPro>();
            ctmp.font = prefabComponent.font;
            ctmp.fontSharedMaterial = prefabComponent.fontSharedMaterial;
            ctmp.color = prefabComponent.color;
        }
        ctmp.fontSize = FontSize ?? 12f;
        ctmp.fontSizeMin = 18f;
        ctmp.fontSizeMax = 72f;
        ctmp.text = Text ?? "Default Text";
        ctmp.fontWeight = FontWeight ?? ctmp.fontWeight;
        ctmp.alignment = TextAlignment ?? ctmp.alignment;

        go.SetActive(true);
        return go;
    }

    [CanBeNull] public string Text { get; set; }
    public TextAlignmentOptions? TextAlignment { get; set; }
    public bool? RichText { get; set; }
    public float? FontSize { get; set; }
    public FontWeight? FontWeight { get; set; }
}