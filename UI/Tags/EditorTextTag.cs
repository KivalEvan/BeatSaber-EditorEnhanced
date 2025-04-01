using BeatmapEditor3D;
using EditorEnhanced.UI.Interfaces;
using HMUI;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace EditorEnhanced.UI.Tags;

public class EditorTextBuilder(BeatmapFlowCoordinator bfc)
{
    public EditorTextTag CreateNew()
    {
        return new EditorTextTag(bfc);
    }
}

public class EditorTextTag(BeatmapFlowCoordinator bfc) : IUIText
{
    public string[] Aliases => ["editor-text", "editor-label"];

    private GameObject PrefabText =>
        bfc._editBeatmapViewController._debugView._currentOverdrawText.gameObject;
    
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

        go.SetActive(true);
        return go;
    }

    [CanBeNull] public string Text { get; set; }
    public TextAlignmentOptions? TextAlignment { get; set; }
    public bool? RichText { get; set; }
    public float? FontSize { get; set; }
    public FontWeight? FontWeight { get; set; }
}