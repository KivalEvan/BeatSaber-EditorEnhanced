using BeatmapEditor3D;
using EditorEnhanced.UI.Interfaces;
using HMUI;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace EditorEnhanced.UI.Tags;

public class EditorTextBuilder
{
    private readonly EditBeatmapViewController _ebvc;

    public EditorTextBuilder(EditBeatmapViewController ebvc)
    {
        _ebvc = ebvc;
    }

    public EditorTextTag CreateNew()
    {
        return new EditorTextTag(_ebvc);
    }
}

public class EditorTextTag : IUIText
{
    private readonly EditBeatmapViewController _ebvc;

    public EditorTextTag(EditBeatmapViewController ebvc)
    {
        _ebvc = ebvc;
    }

    public string[] Aliases => ["editor-text", "editor-label"];

    private GameObject PrefabText =>
        _ebvc._activeSelectionView._arcsCountText.gameObject;

    [CanBeNull] public string Text { get; set; }
    public TextAlignmentOptions? TextAlignment { get; set; }
    public bool? RichText { get; set; }
    public float? FontSize { get; set; }
    public FontWeight? FontWeight { get; set; }

    public GameObject CreateObject(Transform parent)
    {
        var go = new GameObject("EEEditorText")
        {
            layer = 5
        };
        go.SetActive(false);
        go.transform.SetParent(parent, false);

        var ctmp = go.AddComponent<CurvedTextMeshPro>();
        var prefabCtmp = PrefabText.GetComponent<CurvedTextMeshPro>();
        ctmp.font = prefabCtmp.font;
        ctmp.fontSharedMaterial = prefabCtmp.fontSharedMaterial;
        ctmp.color = prefabCtmp.color;
        ctmp.fontSize = FontSize ?? 12f;
        ctmp.fontSizeMin = 18f;
        ctmp.fontSizeMax = 72f;
        ctmp.text = Text ?? "Default Text";
        ctmp.fontWeight = FontWeight ?? ctmp.fontWeight;
        ctmp.alignment = TextAlignment ?? ctmp.alignment;

        ctmp.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        ctmp.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        go.SetActive(true);
        return go;
    }
}